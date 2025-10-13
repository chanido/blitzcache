using System;

namespace BlitzCacheCore.Statistics.Memory
{
    /// <summary>
    /// A basic, zero-dependency, best-effort sizer for common .NET types.
    /// </summary>
    internal sealed class ApproximateValueSizer : IValueSizer
    {
        private const long FallbackSizeBytes = 128; // conservative estimate for unknown reference types
        private const int ObjectOverhead = 24; // rough header+length field approximation for typical CLR objects/arrays on 64-bit
        private static long WithOverhead(long payload) => payload + ObjectOverhead; // helper to keep formulas consistent
        private static long Align8(long value) { long r = value & 7; return r == 0 ? value : value + (8 - r); }
        private static long Align8WithOverhead(long value) => Align8(WithOverhead(value));

        private static readonly System.Collections.Generic.Dictionary<Type, int> PrimitiveWidths = new System.Collections.Generic.Dictionary<Type, int>
        {
            { typeof(bool), 1 }, { typeof(byte), 1 }, { typeof(sbyte), 1 },
            { typeof(char), 2 }, { typeof(short), 2 }, { typeof(ushort), 2 },
            { typeof(int), 4 }, { typeof(uint), 4 }, { typeof(float), 4 },
            { typeof(long), 8 }, { typeof(ulong), 8 }, { typeof(double), 8 },
            { typeof(decimal), 16 }, { typeof(Guid), 16 }, { typeof(DateTime), 8 }, { typeof(TimeSpan), 8 }
        };

        public long GetSizeBytes(object? value)
        {
            // Fast path primitive scalars (approx boxed size): header + payload rounded.
            switch (value)
            {
                case null: return 0;
                case bool _: return Align8WithOverhead(1);
                case byte _: return Align8WithOverhead(1);
                case sbyte _: return Align8WithOverhead(1);
                case char _: return Align8WithOverhead(2);
                case short _: return Align8WithOverhead(2);
                case ushort _: return Align8WithOverhead(2);
                case int _: return Align8WithOverhead(4);
                case uint _: return Align8WithOverhead(4);
                case float _: return Align8WithOverhead(4);
                case long _: return Align8WithOverhead(8);
                case ulong _: return Align8WithOverhead(8);
                case double _: return Align8WithOverhead(8);
                case decimal _: return Align8WithOverhead(16);
                case Guid _: return Align8WithOverhead(16);
                case DateTime _: return Align8WithOverhead(8);
                case TimeSpan _: return Align8WithOverhead(8);
                case string s: return Align8WithOverhead(s.Length * 2L);
            }

            if (value is Array arr)
            {
                var elemType = arr.GetType().GetElementType();
                int len = arr.Length;
                if (elemType == null) return Align8(FallbackSizeBytes); // unknown
                if (len == 0) return Align8(ObjectOverhead); // just header

                // Common primitive element arrays
                if (elemType == typeof(byte) || elemType == typeof(sbyte)) return Align8WithOverhead(len);
                if (elemType == typeof(bool)) return Align8WithOverhead(len); // 1 byte each (approx)
                if (elemType == typeof(char) || elemType == typeof(short) || elemType == typeof(ushort)) return Align8WithOverhead(len * 2L);
                if (elemType == typeof(int) || elemType == typeof(uint) || elemType == typeof(float)) return Align8WithOverhead(len * 4L);
                if (elemType == typeof(long) || elemType == typeof(ulong) || elemType == typeof(double)) return Align8WithOverhead(len * 8L);
                if (elemType == typeof(decimal) || elemType == typeof(Guid)) return Align8WithOverhead(len * 16L);

                // Fallback: assume reference array -> pointer per element
                return Align8WithOverhead(len * IntPtr.Size);
            }

            // Handle Memory<T> / ReadOnlyMemory<T> (boxed structs). Only for primitive-like T.
            var t = value.GetType();
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(Memory<>) || def == typeof(ReadOnlyMemory<>))
                {
                    var elem = t.GetGenericArguments()[0];
                    int width;
                    if (PrimitiveWidths.TryGetValue(elem, out var w)) width = w; else width = IntPtr.Size; // pointer-sized for ref types
                    // Access Length property via reflection (cheap after JIT; could cache if needed)
                    try
                    {
                        var lenProp = t.GetProperty("Length");
                        if (lenProp != null)
                        {
                            var lenObj = lenProp.GetValue(value);
                            if (lenObj is int mlen && mlen >= 0)
                            {
                                return Align8WithOverhead((long)mlen * width);
                            }
                        }
                    }
                    catch { }
                }
                // Handle List<T> primitive T: Count * element width + overhead (pointer per element for refs)
                if (def == typeof(System.Collections.Generic.List<>))
                {
                    var elem = t.GetGenericArguments()[0];
                    long per;
                    if (PrimitiveWidths.TryGetValue(elem, out var w)) per = w; else per = IntPtr.Size; // treat reference as pointer
                    try
                    {
                        var countProp = t.GetProperty("Count");
                        if (countProp != null)
                        {
                            var cntObj = countProp.GetValue(value);
                            if (cntObj is int cnt && cnt >= 0)
                            {
                                return Align8WithOverhead(cnt * per);
                            }
                        }
                    }
                    catch { }
                }
            }

            // Unknown reference type
            return Align8(FallbackSizeBytes);
        }
    }
}
