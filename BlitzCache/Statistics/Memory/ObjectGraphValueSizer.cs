using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using BlitzCacheCore.Statistics.Memory;

namespace BlitzCacheCore.Statistics.Memory
{
    /// <summary>
    /// Bounded recursive object graph sizer. Provides a better approximation than the simple fallback
    /// by walking fields up to a configured depth and sampling collections. Cycle safe and allocation light after warmup.
    /// </summary>
    internal sealed class ObjectGraphValueSizer : IValueSizer
    {
        private readonly ObjectGraphSizerOptions _options;
    private readonly ConcurrentDictionary<Type, TypeLayoutInfo> _layoutCache = new ConcurrentDictionary<Type, TypeLayoutInfo>();

    private static readonly Dictionary<Type, int> PrimitiveValueSizes = new Dictionary<Type, int>()
        {
            { typeof(bool), 1 }, { typeof(byte), 1 }, { typeof(sbyte), 1 },
            { typeof(short), 2 }, { typeof(ushort), 2 }, { typeof(char), 2 },
            { typeof(int), 4 }, { typeof(uint), 4 }, { typeof(float), 4 },
            { typeof(long), 8 }, { typeof(ulong), 8 }, { typeof(double), 8 },
            { typeof(IntPtr), IntPtr.Size }, { typeof(UIntPtr), IntPtr.Size },
            { typeof(decimal), 16 }, { typeof(Guid), 16 }, { typeof(DateTime), 8 },
            { typeof(TimeSpan), 8 }
        };

        public ObjectGraphValueSizer(ObjectGraphSizerOptions? options = null)
        {
            _options = options ?? new ObjectGraphSizerOptions();
        }

        public long GetSizeBytes(object? value)
        {
            if (value is null) return 0;
            if (value is string s) return SizeOfString(s);
            var type = value.GetType();
            if (IsPrimitiveLike(type)) return GetPrimitiveLikeSize(type);
            if (type.IsArray) return SizeOfArray((Array)value, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));

            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            return EstimateObject(value, 0, visited);
        }

        private long EstimateObject(object obj, int depth, HashSet<object> visited)
        {
            if (obj is null) return 0;
            if (!visited.Add(obj)) return 0;
            if (visited.Count > _options.MaxObjects) return 0;

            var type = obj.GetType();

            if (IsPrimitiveLike(type)) return GetPrimitiveLikeSize(type);
            if (obj is string s) return SizeOfString(s);
            if (type.IsArray) return SizeOfArray((Array)obj, depth, visited);

            if (TryCollection(obj, type, depth, visited, out var collectionSize))
                return collectionSize;

            if (type.IsValueType) return EstimateStruct(obj, type, depth, visited);

            var layout = GetLayout(type);
            // Fast mode: return precomputed shallow size immediately
            if (_options.Mode == SizeComputationMode.Fast)
                return layout.PrecomputedShallowSize;

            long total = _options.ObjectHeaderSize;
            int effectiveMaxDepth = _options.MaxDepth + (_options.Mode == SizeComputationMode.Accurate ? 1 : 0);
            if (_options.Mode == SizeComputationMode.Adaptive)
            {
                // Adaptive stays within base MaxDepth (does not use Accurate's +1) to bound size below Accurate.
                effectiveMaxDepth = _options.MaxDepth;
            }
            bool adaptive = _options.Mode == SizeComputationMode.Adaptive;
            if (depth >= effectiveMaxDepth && !adaptive) return Align8(total);

            // In Adaptive mode we decide per-field whether to traverse deeper using a simple heuristic budget.
            // Budget scales with remaining object cap and depth to avoid runaway traversal.
            int remainingObjectBudget = _options.MaxObjects - visited.Count;
            bool allowDeeper = !adaptive || (remainingObjectBudget > 0 && depth < _options.MaxDepth);
            foreach (var f in layout.InstanceFields)
            {
                var ft = f.FieldType;
                object? fv = null;
                try { fv = f.GetValue(obj); } catch { }

                if (ft.IsValueType)
                {
                    total += EstimateValueTypeInline(ft, fv, depth, visited);
                }
                else
                {
                    total += _options.ReferenceSize;
                    if (fv != null)
                    {
                        if (!adaptive)
                        {
                            if (depth < effectiveMaxDepth) total += EstimateObject(fv, depth + 1, visited);
                        }
                        else if (allowDeeper)
                        {
                            // Heuristic: only dive if referenced object's shallow layout suggests substantial size potential
                            var childLayout = GetLayout(fv.GetType());
                            if (childLayout.HasReferenceFields || childLayout.PrecomputedShallowSize > (_options.ObjectHeaderSize + 32))
                            {
                                if (depth < effectiveMaxDepth) total += EstimateObject(fv, depth + 1, visited);
                            }
                        }
                    }
                }
            }
            return Align8(total);
        }

        private long EstimateStruct(object structValue, Type type, int depth, HashSet<object> visited)
        {
            if (IsPrimitiveLike(type)) return GetPrimitiveLikeSize(type);
            int effectiveMaxDepth = _options.MaxDepth + (_options.Mode == SizeComputationMode.Accurate ? 1 : 0);
            if (_options.Mode == SizeComputationMode.Fast) return _options.FallbackStructSize; // treat as shallow approximation
            if (depth >= effectiveMaxDepth || !_options.ReflectIntoStructs) return _options.FallbackStructSize;

            var layout = GetLayout(type);
            long total = 0;
            foreach (var f in layout.InstanceFields)
            {
                var ft = f.FieldType;
                object? fv = null;
                try { fv = f.GetValue(structValue); } catch { }
                if (ft.IsValueType)
                {
                    total += EstimateValueTypeInline(ft, fv, depth, visited);
                }
                else
                {
                    total += _options.ReferenceSize;
                    if (fv != null && depth < _options.MaxDepth) total += EstimateObject(fv, depth + 1, visited);
                }
            }
            return Align8(total);
        }

        private long EstimateValueTypeInline(Type structType, object? value, int depth, HashSet<object> visited)
        {
            if (IsPrimitiveLike(structType)) return GetPrimitiveLikeSize(structType);
            if (value is null) return _options.FallbackStructSize;
            return EstimateStruct(value, structType, depth + 1, visited);
        }

        private long SizeOfString(string s) => Align8(_options.StringOverhead + (s.Length * 2L));

    private long SizeOfArray(Array arr, int depth, HashSet<object> visited)
        {
            var elemType = arr.GetType().GetElementType()!;
            long size = _options.ArrayHeaderSize;
            int len = arr.Length;

            if (IsPrimitiveLike(elemType))
            {
                size += (long)len * GetPrimitiveLikeSize(elemType);
                return Align8(size);
            }

            if (elemType.IsValueType && !_options.ReflectIntoStructs)
            {
                size += (long)len * _options.FallbackStructSize;
                return Align8(size);
            }

            int sampleLimit = _options.Mode == SizeComputationMode.Accurate ? _options.MaxSampledElementsPerLevel * 2 : _options.MaxSampledElementsPerLevel;
            if (_options.Mode == SizeComputationMode.Fast) sampleLimit = Math.Min(sampleLimit, 4); // very small sample
            if (_options.Mode == SizeComputationMode.Adaptive)
            {
                // Adaptive: smaller than Balanced for large arrays (cap at 0.75 * base) to ensure Accurate remains largest.
                int cap = (int)Math.Ceiling(_options.MaxSampledElementsPerLevel * 0.75);
                sampleLimit = Math.Min(sampleLimit / 2, cap);
            }
            int toSample = Math.Min(len, sampleLimit);

            if (elemType.IsValueType)
            {
                long sampleTotal = 0;
                for (int i = 0; i < toSample; i++)
                {
                    var v = arr.GetValue(i);
                    sampleTotal += EstimateValueTypeInline(elemType, v, depth, visited);
                }
                if (toSample > 0)
                {
                    long avg = sampleTotal / toSample;
                    size += avg * len;
                }
                return Align8(size);
            }
            else
            {
                size += (long)len * _options.ReferenceSize;
                int effectiveMaxDepth = _options.MaxDepth + (_options.Mode == SizeComputationMode.Accurate ? 1 : 0);
                if (_options.Mode == SizeComputationMode.Fast)
                {
                    // Fast mode: do not traverse reference elements
                    return Align8(size);
                }
                if (_options.Mode == SizeComputationMode.Adaptive)
                {
                    // Adaptive: traverse only a small subset (cube root) to stay lighter than Balanced for large arrays
                    int traverse = (int)Math.Ceiling(Math.Pow(toSample, 1.0 / 3.0));
                    for (int i = 0; i < traverse; i++)
                    {
                        var v = arr.GetValue(i);
                        if (v != null && depth < effectiveMaxDepth) size += EstimateObject(v, depth + 1, visited);
                    }
                    return Align8(size);
                }
                if (depth < effectiveMaxDepth)
                {
                    for (int i = 0; i < toSample; i++)
                    {
                        var v = arr.GetValue(i);
                        if (v != null) size += EstimateObject(v, depth + 1, visited);
                    }
                }
                return Align8(size);
            }
        }

        private bool TryCollection(object obj, Type type, int depth, HashSet<object> visited, out long size)
        {
            size = 0;
            if (obj is string) return false;

            if (obj is IDictionary dict)
            {
                size = _options.ObjectHeaderSize;
                int count = dict.Count;
                int sampled = 0;
                int sampleLimit = _options.Mode == SizeComputationMode.Accurate ? _options.MaxSampledElementsPerLevel * 2 : _options.MaxSampledElementsPerLevel;
                if (_options.Mode == SizeComputationMode.Fast) sampleLimit = Math.Min(sampleLimit, 4);
                if (_options.Mode == SizeComputationMode.Adaptive)
                {
                    int cap = (int)Math.Ceiling(_options.MaxSampledElementsPerLevel * 0.75);
                    sampleLimit = Math.Min(sampleLimit / 2, cap);
                }
                foreach (DictionaryEntry de in dict)
                {
                    if (sampled++ >= sampleLimit) break;
                    size += _options.ReferenceSize * 2;
                    if (_options.Mode != SizeComputationMode.Fast && _options.Mode != SizeComputationMode.Adaptive)
                    {
                        int effectiveMaxDepth = _options.MaxDepth + (_options.Mode == SizeComputationMode.Accurate ? 1 : 0);
                        if (depth < effectiveMaxDepth)
                        {
                            if (de.Key != null) size += EstimateObject(de.Key, depth + 1, visited);
                            if (de.Value != null) size += EstimateObject(de.Value, depth + 1, visited);
                        }
                    }
                    else if (_options.Mode == SizeComputationMode.Adaptive)
                    {
                        // Adaptive: traverse only if key/value shallow layout indicates complexity and depth budget allows
                        if (depth < _options.MaxDepth)
                        {
                            if (de.Key != null)
                            {
                                var kl = GetLayout(de.Key.GetType());
                                if (kl.HasReferenceFields) size += EstimateObject(de.Key, depth + 1, visited);
                            }
                            if (de.Value != null)
                            {
                                var vl = GetLayout(de.Value.GetType());
                                if (vl.HasReferenceFields) size += EstimateObject(de.Value, depth + 1, visited);
                            }
                        }
                    }
                }
                if (count > sampled)
                {
                    int remaining = count - sampled;
                    size += remaining * (_options.ReferenceSize * 2);
                }
                return true;
            }

            if (obj is IEnumerable enumerable)
            {
                size = _options.ObjectHeaderSize;
                int? count = (obj as ICollection)?.Count;
                int sampled = 0;
                int sampleLimit = _options.Mode == SizeComputationMode.Accurate ? _options.MaxSampledElementsPerLevel * 2 : _options.MaxSampledElementsPerLevel;
                if (_options.Mode == SizeComputationMode.Fast) sampleLimit = Math.Min(sampleLimit, 8); // allow a few elements
                if (_options.Mode == SizeComputationMode.Adaptive && count.HasValue)
                {
                    int cap = (int)Math.Ceiling(_options.MaxSampledElementsPerLevel * 0.75);
                    sampleLimit = Math.Min(sampleLimit / 2, cap);
                }
                foreach (var item in enumerable)
                {
                    if (sampled++ >= sampleLimit) break;
                    size += _options.ReferenceSize;
                    if (_options.Mode != SizeComputationMode.Fast && _options.Mode != SizeComputationMode.Adaptive)
                    {
                        int effectiveMaxDepth = _options.MaxDepth + (_options.Mode == SizeComputationMode.Accurate ? 1 : 0);
                        if (item != null && depth < effectiveMaxDepth) size += EstimateObject(item, depth + 1, visited);
                    }
                    else if (_options.Mode == SizeComputationMode.Adaptive && item != null && depth < _options.MaxDepth)
                    {
                        var il = GetLayout(item.GetType());
                        if (il.HasReferenceFields) size += EstimateObject(item, depth + 1, visited);
                    }
                }
                if (count.HasValue && count > sampled)
                {
                    int remaining = count.Value - sampled;
                    size += remaining * _options.ReferenceSize;
                }
                return true;
            }
            return false;
        }

        private static bool IsPrimitiveLike(Type t) => t.IsPrimitive || PrimitiveValueSizes.ContainsKey(t) || t.IsEnum;

        private static int GetPrimitiveLikeSize(Type t)
        {
            if (t.IsEnum) return GetPrimitiveLikeSize(Enum.GetUnderlyingType(t));
            if (PrimitiveValueSizes.TryGetValue(t, out var s)) return s;
            return t.IsPrimitive ? System.Runtime.InteropServices.Marshal.SizeOf(t) : 0;
        }

        private TypeLayoutInfo GetLayout(Type t)
        {
            return _layoutCache.GetOrAdd(t, tt =>
            {
                var fields = tt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                bool hasRef = false;
                long shallow = _options.ObjectHeaderSize; // header cost for reference types; structs will use inline calc
                foreach (var f in fields)
                {
                    var ft = f.FieldType;
                    if (ft.IsValueType)
                    {
                        if (IsPrimitiveLike(ft)) shallow += GetPrimitiveLikeSize(ft);
                        else if (_options.ReflectIntoStructs)
                            shallow += _options.FallbackStructSize; // approximate inline (could refine later)
                        else
                            shallow += _options.FallbackStructSize;
                    }
                    else
                    {
                        hasRef = true;
                        shallow += _options.ReferenceSize; // pointer cost
                    }
                }
                shallow = Align8(shallow);
                return new TypeLayoutInfo(fields, hasRef, shallow);
            });
        }

        private static long Align8(long value)
        {
            long rem = value & 7;
            return rem == 0 ? value : (value + (8 - rem));
        }

        private sealed class TypeLayoutInfo
        {
            public FieldInfo[] InstanceFields { get; private set; }
            public bool HasReferenceFields { get; private set; }
            public long PrecomputedShallowSize { get; private set; }
            public TypeLayoutInfo(FieldInfo[] fields, bool hasRef, long shallow)
            {
                InstanceFields = fields;
                HasReferenceFields = hasRef;
                PrecomputedShallowSize = shallow;
            }
        }
    }

}
