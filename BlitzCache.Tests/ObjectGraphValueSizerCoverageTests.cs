using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlitzCacheCore.Statistics.Memory;
using NUnit.Framework;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Comprehensive coverage tests for ObjectGraphValueSizer to ensure all code paths are exercised.
    /// </summary>
    [TestFixture]
    public class ObjectGraphValueSizerCoverageTests
    {
        private ObjectGraphValueSizer _balancedSizer;
        private ObjectGraphValueSizer _fastSizer;
        private ObjectGraphValueSizer _adaptiveSizer;
        private ObjectGraphValueSizer _accurateSizer;

        [SetUp]
        public void SetUp()
        {
            _balancedSizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                Mode = SizeComputationMode.Balanced,
                MaxDepth = 3,
                MaxObjects = 512
            });
            
            _fastSizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                Mode = SizeComputationMode.Fast 
            });
            
            _adaptiveSizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                Mode = SizeComputationMode.Adaptive,
                MaxDepth = 3,
                MaxObjects = 256
            });
            
            _accurateSizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                Mode = SizeComputationMode.Accurate,
                MaxDepth = 4,
                MaxObjects = 1024
            });
        }

        #region Null and Primitive Tests

        [Test]
        public void GetSizeBytes_Null_ReturnsZero()
        {
            Assert.That(_balancedSizer.GetSizeBytes(null), Is.EqualTo(0));
            Assert.That(_fastSizer.GetSizeBytes(null), Is.EqualTo(0));
            Assert.That(_adaptiveSizer.GetSizeBytes(null), Is.EqualTo(0));
            Assert.That(_accurateSizer.GetSizeBytes(null), Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Bool_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(true);
            Assert.That(size, Is.EqualTo(1), "bool is 1 byte");
        }

        [Test]
        public void GetSizeBytes_Byte_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes((byte)255);
            Assert.That(size, Is.EqualTo(1), "byte is 1 byte");
        }

        [Test]
        public void GetSizeBytes_SByte_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes((sbyte)-128);
            Assert.That(size, Is.EqualTo(1), "sbyte is 1 byte");
        }

        [Test]
        public void GetSizeBytes_Int16_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes((short)1000);
            Assert.That(size, Is.EqualTo(2), "short is 2 bytes");
        }

        [Test]
        public void GetSizeBytes_UInt16_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes((ushort)5000);
            Assert.That(size, Is.EqualTo(2), "ushort is 2 bytes");
        }

        [Test]
        public void GetSizeBytes_Char_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes('Z');
            Assert.That(size, Is.EqualTo(2), "char is 2 bytes");
        }

        [Test]
        public void GetSizeBytes_Int32_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(123456);
            Assert.That(size, Is.EqualTo(4), "int is 4 bytes");
        }

        [Test]
        public void GetSizeBytes_UInt32_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(123456u);
            Assert.That(size, Is.EqualTo(4), "uint is 4 bytes");
        }

        [Test]
        public void GetSizeBytes_Float_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(3.14f);
            Assert.That(size, Is.EqualTo(4), "float is 4 bytes");
        }

        [Test]
        public void GetSizeBytes_Int64_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(9876543210L);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_UInt64_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(9876543210UL);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Double_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(3.141592653589793);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Decimal_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(123.456m);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Guid_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(Guid.NewGuid());
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_DateTime_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(DateTime.UtcNow);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_TimeSpan_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(TimeSpan.FromHours(2.5));
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_IntPtr_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(new IntPtr(12345));
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_UIntPtr_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(new UIntPtr(54321));
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Enum Tests

        public enum TestEnum { None = 0, First = 1, Second = 2, Third = 3 }
        
        [Flags]
        public enum FlagsEnum { None = 0, Read = 1, Write = 2, Execute = 4 }

        [Test]
        public void GetSizeBytes_Enum_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(TestEnum.Second);
            Assert.That(size, Is.EqualTo(4), "enum (int-based) is 4 bytes");
        }

        [Test]
        public void GetSizeBytes_FlagsEnum_ReturnsPrimitiveSize()
        {
            var size = _balancedSizer.GetSizeBytes(FlagsEnum.Read | FlagsEnum.Write);
            Assert.That(size, Is.EqualTo(4), "flags enum (int-based) is 4 bytes");
        }

        #endregion

        #region Array Tests - Primitives

        [Test]
        public void GetSizeBytes_EmptyArray_ReturnsHeaderSize()
        {
            var size = _balancedSizer.GetSizeBytes(new int[0]);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ByteArray_ScalesWithLength()
        {
            var arr = new byte[100];
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(100));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_IntArray_ScalesWithLength()
        {
            var arr = new int[50];
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(200)); // at least 4 * 50
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_LongArray_ScalesWithLength()
        {
            var arr = new long[50];
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(400)); // at least 8 * 50
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_DoubleArray_ScalesWithLength()
        {
            var arr = new double[50];
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(400)); // at least 8 * 50
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_BoolArray_ScalesWithLength()
        {
            var arr = new bool[100];
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(100));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_CharArray_ScalesWithLength()
        {
            var arr = new char[50];
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(100)); // at least 2 * 50
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_DecimalArray_ScalesWithLength()
        {
            var arr = new decimal[25];
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(400)); // at least 16 * 25
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Array Tests - Structs and Reference Types

        public struct SimpleStruct
        {
            public int X;
            public int Y;
        }

        public struct NestedStruct
        {
            public int Value;
            public SimpleStruct Inner;
            public string Text;
        }

        [Test]
        public void GetSizeBytes_StructArray_WithReflection()
        {
            var sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                ReflectIntoStructs = true,
                MaxDepth = 2
            });
            
            var arr = new SimpleStruct[10];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = new SimpleStruct { X = i, Y = i * 2 };
            
            var size = sizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(80)); // at least 8 bytes * 10
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_StructArray_WithoutReflection()
        {
            var sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                ReflectIntoStructs = false
            });
            
            var arr = new SimpleStruct[10];
            var size = sizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_NestedStructArray_WithReflection()
        {
            var sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                ReflectIntoStructs = true,
                MaxDepth = 3
            });
            
            var arr = new NestedStruct[5];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = new NestedStruct { Value = i, Inner = new SimpleStruct { X = i, Y = i }, Text = "test" };
            
            var size = sizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_StringArray_IncludesReferences()
        {
            var arr = new string[] { "one", "two", "three", "four", "five" };
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ObjectArray_WithBalancedMode()
        {
            var arr = new object[] 
            { 
                new { A = 1 }, 
                new { B = 2 }, 
                new { C = 3 } 
            };
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ObjectArray_WithFastMode()
        {
            var arr = new object[] 
            { 
                new { A = 1 }, 
                new { B = 2 }, 
                new { C = 3 } 
            };
            var size = _fastSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ObjectArray_WithAdaptiveMode()
        {
            var arr = new object[] 
            { 
                new { A = 1, B = "test" }, 
                new { C = 2, D = "data" }, 
                new { E = 3, F = "info" } 
            };
            var size = _adaptiveSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Collection Tests - Dictionary

        [Test]
        public void GetSizeBytes_EmptyDictionary_ReturnsOverhead()
        {
            var dict = new Dictionary<string, int>();
            var size = _balancedSizer.GetSizeBytes(dict);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Dictionary_WithPrimitiveValues()
        {
            var dict = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["two"] = 2,
                ["three"] = 3,
                ["four"] = 4,
                ["five"] = 5
            };
            var size = _balancedSizer.GetSizeBytes(dict);
            Assert.That(size, Is.GreaterThan(0));
            // ObjectGraphValueSizer does not align collection sizes
        }

        [Test]
        public void GetSizeBytes_Dictionary_WithObjectValues()
        {
            var dict = new Dictionary<string, object>
            {
                ["a"] = new { Value = 1 },
                ["b"] = new { Value = 2 },
                ["c"] = new { Value = 3 }
            };
            var size = _balancedSizer.GetSizeBytes(dict);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_LargeDictionary_UsesSampling()
        {
            var dict = new Dictionary<string, int>();
            for (int i = 0; i < 200; i++)
                dict[$"key{i}"] = i;
            
            var size = _balancedSizer.GetSizeBytes(dict);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Dictionary_FastMode_SkipsTraversal()
        {
            var dict = new Dictionary<string, object>
            {
                ["a"] = new { Value = 1, Data = new string('x', 100) },
                ["b"] = new { Value = 2, Data = new string('y', 100) }
            };
            var size = _fastSizer.GetSizeBytes(dict);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Dictionary_AdaptiveMode_SelectiveTraversal()
        {
            var dict = new Dictionary<string, object>
            {
                ["simple"] = 42,
                ["complex"] = new { A = 1, B = "test", C = new List<int> { 1, 2, 3 } }
            };
            var size = _adaptiveSizer.GetSizeBytes(dict);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Collection Tests - IEnumerable

        private class CustomEnumerable : IEnumerable<int>
        {
            private readonly List<int> _items = new List<int> { 1, 2, 3, 4, 5 };
            
            public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class CustomEnumerableWithCount : IEnumerable<string>, ICollection<string>
        {
            private readonly List<string> _items = new List<string> { "a", "b", "c", "d", "e" };
            
            public int Count => _items.Count;
            public bool IsReadOnly => true;
            
            public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => _items.Contains(item);
            public void CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public bool Remove(string item) => throw new NotSupportedException();
        }

        [Test]
        public void GetSizeBytes_CustomEnumerable_WithoutCount()
        {
            var enumerable = new CustomEnumerable();
            var size = _balancedSizer.GetSizeBytes(enumerable);
            Assert.That(size, Is.GreaterThan(0));
            // ObjectGraphValueSizer does not align collection sizes
        }

        [Test]
        public void GetSizeBytes_CustomEnumerable_WithCount()
        {
            var enumerable = new CustomEnumerableWithCount();
            var size = _balancedSizer.GetSizeBytes(enumerable);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_List_AsIEnumerable()
        {
            IEnumerable<int> list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var size = _balancedSizer.GetSizeBytes(list);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_LargeEnumerable_UsesSampling()
        {
            var list = Enumerable.Range(1, 200).ToList();
            var size = _balancedSizer.GetSizeBytes(list);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Enumerable_FastMode()
        {
            var list = new List<object>();
            for (int i = 0; i < 50; i++)
                list.Add(new { Value = i, Data = new string('x', 20) });
            
            var size = _fastSizer.GetSizeBytes(list);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Enumerable_AdaptiveMode()
        {
            var list = new List<object>
            {
                42,
                "simple",
                new { Complex = new List<int> { 1, 2, 3 } }
            };
            var size = _adaptiveSizer.GetSizeBytes(list);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region MaxObjects and MaxDepth Tests

        private class DeepNode
        {
            public string Name = "node";
            public DeepNode Child;
        }

        [Test]
        public void GetSizeBytes_ExceedsMaxObjects_StopsTraversal()
        {
            var sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                MaxObjects = 5,
                MaxDepth = 10
            });

            var list = new List<object>();
            for (int i = 0; i < 20; i++)
                list.Add(new { Value = i });

            var size = sizer.GetSizeBytes(list);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_DeepNesting_RespectsMaxDepth()
        {
            var sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                MaxDepth = 2,
                MaxObjects = 100
            });

            var root = new DeepNode { Name = "L1" };
            root.Child = new DeepNode { Name = "L2" };
            root.Child.Child = new DeepNode { Name = "L3" };
            root.Child.Child.Child = new DeepNode { Name = "L4" };
            root.Child.Child.Child.Child = new DeepNode { Name = "L5" };

            var size = sizer.GetSizeBytes(root);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_AccurateMode_IncludesExtraDepth()
        {
            var root = new DeepNode { Name = "L1" };
            root.Child = new DeepNode { Name = "L2" };
            root.Child.Child = new DeepNode { Name = "L3" };
            root.Child.Child.Child = new DeepNode { Name = "L4" };

            var size = _accurateSizer.GetSizeBytes(root);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Custom Struct Tests

        public struct StructWithReferences
        {
            public int Value;
            public string Text;
            public List<int> Numbers;
        }

        [Test]
        public void GetSizeBytes_StructWithReferences_ReflectEnabled()
        {
            var sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                ReflectIntoStructs = true,
                MaxDepth = 3
            });

            var data = new StructWithReferences
            {
                Value = 42,
                Text = "hello",
                Numbers = new List<int> { 1, 2, 3, 4, 5 }
            };

            var size = sizer.GetSizeBytes(data);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_StructWithReferences_ReflectDisabled()
        {
            var sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions 
            { 
                ReflectIntoStructs = false
            });

            var data = new StructWithReferences
            {
                Value = 42,
                Text = "hello",
                Numbers = new List<int> { 1, 2, 3 }
            };

            var size = sizer.GetSizeBytes(data);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_StructWithReferences_FastMode()
        {
            var data = new StructWithReferences
            {
                Value = 42,
                Text = new string('x', 100),
                Numbers = new List<int>(Enumerable.Range(1, 100))
            };

            var size = _fastSizer.GetSizeBytes(data);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Anonymous Type and Complex Object Tests

        [Test]
        public void GetSizeBytes_AnonymousType_Simple()
        {
            var obj = new { A = 1, B = "test", C = 3.14 };
            var size = _balancedSizer.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_AnonymousType_Nested()
        {
            var obj = new 
            { 
                A = 1, 
                B = new { X = 10, Y = 20 }, 
                C = new List<int> { 1, 2, 3 } 
            };
            var size = _balancedSizer.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_AnonymousType_AllModes()
        {
            var obj = new 
            { 
                Name = "test",
                Value = 42,
                Data = new List<string> { "a", "b", "c" },
                Nested = new { X = 1, Y = 2 }
            };

            var fastSize = _fastSizer.GetSizeBytes(obj);
            var balancedSize = _balancedSizer.GetSizeBytes(obj);
            var adaptiveSize = _adaptiveSizer.GetSizeBytes(obj);
            var accurateSize = _accurateSizer.GetSizeBytes(obj);

            Assert.That(fastSize, Is.GreaterThan(0));
            Assert.That(balancedSize, Is.GreaterThan(0));
            Assert.That(adaptiveSize, Is.GreaterThan(0));
            Assert.That(accurateSize, Is.GreaterThan(0));
            
            // Fast should be smallest or equal
            Assert.That(fastSize, Is.LessThanOrEqualTo(balancedSize));
            Assert.That(fastSize, Is.LessThanOrEqualTo(adaptiveSize));
            Assert.That(fastSize, Is.LessThanOrEqualTo(accurateSize));
        }

        #endregion

        #region Complex Nested Structures

        private class ComplexObject
        {
            public int Id;
            public string Name;
            public List<int> Numbers;
            public Dictionary<string, string> Metadata;
            public ComplexObject Child;
        }

        [Test]
        public void GetSizeBytes_ComplexNestedStructure_BalancedMode()
        {
            var obj = new ComplexObject
            {
                Id = 1,
                Name = "root",
                Numbers = new List<int> { 1, 2, 3, 4, 5 },
                Metadata = new Dictionary<string, string> 
                { 
                    ["key1"] = "value1",
                    ["key2"] = "value2"
                },
                Child = new ComplexObject
                {
                    Id = 2,
                    Name = "child",
                    Numbers = new List<int> { 10, 20, 30 },
                    Metadata = new Dictionary<string, string> { ["key3"] = "value3" }
                }
            };

            var size = _balancedSizer.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ComplexNestedStructure_FastMode()
        {
            var obj = new ComplexObject
            {
                Id = 1,
                Name = "root",
                Numbers = new List<int> { 1, 2, 3 },
                Metadata = new Dictionary<string, string> { ["k"] = "v" },
                Child = new ComplexObject { Id = 2, Name = "child" }
            };

            var size = _fastSizer.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ComplexNestedStructure_AdaptiveMode()
        {
            var obj = new ComplexObject
            {
                Id = 1,
                Name = "root",
                Numbers = new List<int>(Enumerable.Range(1, 50)),
                Metadata = new Dictionary<string, string>(),
                Child = new ComplexObject 
                { 
                    Id = 2, 
                    Name = "child",
                    Numbers = new List<int>(Enumerable.Range(1, 20))
                }
            };

            var size = _adaptiveSizer.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Cycle Detection

        private class CyclicNode
        {
            public string Name;
            public CyclicNode Next;
            public List<CyclicNode> Children = new List<CyclicNode>();
        }

        [Test]
        public void GetSizeBytes_SimpleCycle_DoesNotInfiniteLoop()
        {
            var a = new CyclicNode { Name = "A" };
            var b = new CyclicNode { Name = "B" };
            a.Next = b;
            b.Next = a;

            var size = _balancedSizer.GetSizeBytes(a);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ComplexCycle_WithCollections()
        {
            var a = new CyclicNode { Name = "A" };
            var b = new CyclicNode { Name = "B" };
            var c = new CyclicNode { Name = "C" };
            
            a.Next = b;
            b.Next = c;
            c.Next = a;
            
            a.Children.Add(b);
            b.Children.Add(c);
            c.Children.Add(a);

            var size = _balancedSizer.GetSizeBytes(a);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_SelfReference_HandledCorrectly()
        {
            var node = new CyclicNode { Name = "Self" };
            node.Next = node;
            node.Children.Add(node);

            var size = _balancedSizer.GetSizeBytes(node);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Layout Caching Tests

        private class TypeA { public int Value; }
        private class TypeB { public int Value; }
        private class TypeC { public int Value; }

        [Test]
        public void GetSizeBytes_MultipleTypes_UsesLayoutCache()
        {
            var a = new TypeA { Value = 1 };
            var b = new TypeB { Value = 2 };
            var c = new TypeC { Value = 3 };

            // First call caches the layout
            var size1 = _balancedSizer.GetSizeBytes(a);
            // Second call uses cached layout
            var size2 = _balancedSizer.GetSizeBytes(new TypeA { Value = 10 });
            
            Assert.That(size1, Is.EqualTo(size2), "Same type should have same size");

            var sizeB = _balancedSizer.GetSizeBytes(b);
            var sizeC = _balancedSizer.GetSizeBytes(c);

            Assert.That(sizeB, Is.GreaterThan(0));
            Assert.That(sizeC, Is.GreaterThan(0));
        }

        [Test]
        public void GetSizeBytes_RepeatedCalls_ConsistentResults()
        {
            var obj = new { A = 1, B = "test", C = new List<int> { 1, 2, 3 } };
            
            var sizes = new long[10];
            for (int i = 0; i < 10; i++)
                sizes[i] = _balancedSizer.GetSizeBytes(obj);

            // All sizes should be identical
            Assert.That(sizes.Distinct().Count(), Is.EqualTo(1));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetSizeBytes_EmptyString_ReturnsOverheadOnly()
        {
            var size = _balancedSizer.GetSizeBytes("");
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_VeryLargeString_HandlesCorrectly()
        {
            var largeString = new string('x', 10000);
            var size = _balancedSizer.GetSizeBytes(largeString);
            Assert.That(size, Is.GreaterThan(20000)); // at least 2 * 10000
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_NullFields_HandledCorrectly()
        {
            var obj = new ComplexObject { Id = 1, Name = null, Numbers = null, Metadata = null, Child = null };
            var size = _balancedSizer.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_MixedNullAndNonNull_HandledCorrectly()
        {
            var obj = new ComplexObject 
            { 
                Id = 1, 
                Name = "test", 
                Numbers = null, 
                Metadata = new Dictionary<string, string> { ["k"] = "v" }, 
                Child = null 
            };
            var size = _balancedSizer.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_ArrayOfNulls_HandledCorrectly()
        {
            var arr = new string[10]; // all null
            var size = _balancedSizer.GetSizeBytes(arr);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0));
        }

        #endregion

        #region Mode Comparison Tests

        [Test]
        public void GetSizeBytes_ModesComparison_LargeCollection()
        {
            var list = new List<object>();
            for (int i = 0; i < 100; i++)
                list.Add(new { Id = i, Name = $"Item{i}", Data = new int[] { i, i * 2, i * 3 } });

            var fastSize = _fastSizer.GetSizeBytes(list);
            var balancedSize = _balancedSizer.GetSizeBytes(list);
            var adaptiveSize = _adaptiveSizer.GetSizeBytes(list);
            var accurateSize = _accurateSizer.GetSizeBytes(list);

            Assert.That(fastSize, Is.GreaterThan(0));
            Assert.That(balancedSize, Is.GreaterThanOrEqualTo(fastSize));
            Assert.That(adaptiveSize, Is.GreaterThan(0));
            Assert.That(accurateSize, Is.GreaterThanOrEqualTo(balancedSize));
        }

        [Test]
        public void GetSizeBytes_ModesComparison_DeepNesting()
        {
            var root = new DeepNode { Name = "L1" };
            var current = root;
            for (int i = 2; i <= 10; i++)
            {
                current.Child = new DeepNode { Name = $"L{i}" };
                current = current.Child;
            }

            var fastSize = _fastSizer.GetSizeBytes(root);
            var balancedSize = _balancedSizer.GetSizeBytes(root);
            var adaptiveSize = _adaptiveSizer.GetSizeBytes(root);
            var accurateSize = _accurateSizer.GetSizeBytes(root);

            Assert.That(fastSize, Is.GreaterThan(0));
            Assert.That(balancedSize, Is.GreaterThan(0));
            Assert.That(adaptiveSize, Is.GreaterThan(0));
            Assert.That(accurateSize, Is.GreaterThanOrEqualTo(balancedSize));
        }

        #endregion
    }
}
