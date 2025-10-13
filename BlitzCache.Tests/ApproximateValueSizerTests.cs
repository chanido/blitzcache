using System;
using System.Collections.Generic;
using BlitzCacheCore.Statistics.Memory;
using NUnit.Framework;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class ApproximateValueSizerTests
    {
        private ApproximateValueSizer _sizer;

        [SetUp]
        public void SetUp()
        {
            _sizer = new ApproximateValueSizer();
        }

        #region Null and Primitive Types

        [Test]
        public void GetSizeBytes_Null_ReturnsZero()
        {
            var size = _sizer.GetSizeBytes(null);
            Assert.That(size, Is.EqualTo(0));
        }

        [Test]
        public void GetSizeBytes_Bool_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(true);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Byte_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes((byte)42);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_SByte_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes((sbyte)-42);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Char_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes('A');
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Short_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes((short)1000);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_UShort_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes((ushort)1000);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Int_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(100000);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_UInt_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(100000u);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Float_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(3.14f);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Long_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(1000000000L);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ULong_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(1000000000UL);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Double_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(3.14159);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Decimal_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(123.456m);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Guid_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(Guid.NewGuid());
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_DateTime_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(DateTime.UtcNow);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_TimeSpan_ReturnsAlignedSize()
        {
            var size = _sizer.GetSizeBytes(TimeSpan.FromHours(1));
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        #endregion

        #region String Tests

        [Test]
        public void GetSizeBytes_EmptyString_ReturnsOverheadOnly()
        {
            var size = _sizer.GetSizeBytes("");
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ShortString_ScalesWithLength()
        {
            var size5 = _sizer.GetSizeBytes("Hello");
            var size10 = _sizer.GetSizeBytes("HelloWorld");
            
            Assert.That(size10, Is.GreaterThan(size5));
            // Each char is 2 bytes in .NET strings, but alignment may reduce the apparent difference
            var expectedDiff = (10 - 5) * 2;
            Assert.That(size10 - size5, Is.GreaterThanOrEqualTo(expectedDiff - 8), 
                "Size should increase by approximately 2 bytes per character (accounting for alignment)");
        }

        [Test]
        public void GetSizeBytes_LargeString_ScalesLinearly()
        {
            var str100 = new string('x', 100);
            var str200 = new string('x', 200);
            
            var size100 = _sizer.GetSizeBytes(str100);
            var size200 = _sizer.GetSizeBytes(str200);
            
            Assert.That(size200, Is.GreaterThan(size100));
            // Should roughly double (within alignment padding)
            Assert.That(size200, Is.GreaterThanOrEqualTo(size100 * 1.8));
        }

        [Test]
        public void GetSizeBytes_UnicodeString_CountsAllCharacters()
        {
            var unicode = "Hello 世界 🌍";
            var size = _sizer.GetSizeBytes(unicode);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        #endregion

        #region Array Tests

        [Test]
        public void GetSizeBytes_EmptyByteArray_ReturnsOverheadOnly()
        {
            var size = _sizer.GetSizeBytes(new byte[0]);
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ByteArray_ScalesWithLength()
        {
            var arr10 = new byte[10];
            var arr100 = new byte[100];
            
            var size10 = _sizer.GetSizeBytes(arr10);
            var size100 = _sizer.GetSizeBytes(arr100);
            
            Assert.That(size100, Is.GreaterThan(size10));
            // Accounting for 8-byte alignment padding
            Assert.That(size100 - size10, Is.GreaterThanOrEqualTo(88), 
                "Size should increase by approximately 90 bytes (accounting for alignment)");
        }

        [Test]
        public void GetSizeBytes_SByteArray_ScalesWithLength()
        {
            var arr = new sbyte[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(50)); // at least array length + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_BoolArray_ScalesWithLength()
        {
            var arr = new bool[100];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(100)); // at least array length + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_CharArray_Scales2BytesPerElement()
        {
            var arr = new char[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(100)); // at least 2 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ShortArray_Scales2BytesPerElement()
        {
            var arr = new short[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(100)); // at least 2 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_UShortArray_Scales2BytesPerElement()
        {
            var arr = new ushort[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(100)); // at least 2 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_IntArray_Scales4BytesPerElement()
        {
            var arr = new int[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(200)); // at least 4 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_UIntArray_Scales4BytesPerElement()
        {
            var arr = new uint[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(200)); // at least 4 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_FloatArray_Scales4BytesPerElement()
        {
            var arr = new float[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(200)); // at least 4 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_LongArray_Scales8BytesPerElement()
        {
            var arr = new long[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(400)); // at least 8 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ULongArray_Scales8BytesPerElement()
        {
            var arr = new ulong[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(400)); // at least 8 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_DoubleArray_Scales8BytesPerElement()
        {
            var arr = new double[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(400)); // at least 8 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_DecimalArray_Scales16BytesPerElement()
        {
            var arr = new decimal[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(800)); // at least 16 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_GuidArray_Scales16BytesPerElement()
        {
            var arr = new Guid[50];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(800)); // at least 16 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_StringArray_ReturnsPointerSizedEstimate()
        {
            var arr = new string[10];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ObjectArray_ReturnsFallbackSize()
        {
            var arr = new object[10];
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        #endregion

        #region Memory<T> and ReadOnlyMemory<T> Tests

        [Test]
        public void GetSizeBytes_MemoryOfByte_ScalesWithLength()
        {
            var mem = new Memory<byte>(new byte[100]);
            var size = _sizer.GetSizeBytes(mem);
            
            Assert.That(size, Is.GreaterThan(100)); // at least length + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ReadOnlyMemoryOfByte_ScalesWithLength()
        {
            var mem = new ReadOnlyMemory<byte>(new byte[100]);
            var size = _sizer.GetSizeBytes(mem);
            
            Assert.That(size, Is.GreaterThan(100)); // at least length + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_MemoryOfInt_Scales4BytesPerElement()
        {
            var mem = new Memory<int>(new int[50]);
            var size = _sizer.GetSizeBytes(mem);
            
            Assert.That(size, Is.GreaterThan(200)); // at least 4 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_MemoryOfLong_Scales8BytesPerElement()
        {
            var mem = new Memory<long>(new long[50]);
            var size = _sizer.GetSizeBytes(mem);
            
            Assert.That(size, Is.GreaterThan(400)); // at least 8 * 50 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_EmptyMemory_ReturnsOverheadOnly()
        {
            var mem = new Memory<byte>(Array.Empty<byte>());
            var size = _sizer.GetSizeBytes(mem);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        #endregion

        #region List<T> Tests

        [Test]
        public void GetSizeBytes_EmptyListOfInt_ReturnsOverheadOnly()
        {
            var list = new List<int>();
            var size = _sizer.GetSizeBytes(list);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ListOfInt_ScalesWithCount()
        {
            var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var size = _sizer.GetSizeBytes(list);
            
            Assert.That(size, Is.GreaterThan(40)); // at least 4 * 10 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ListOfByte_ScalesWithCount()
        {
            var list = new List<byte> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var size = _sizer.GetSizeBytes(list);
            
            Assert.That(size, Is.GreaterThan(10)); // at least count + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ListOfLong_Scales8BytesPerElement()
        {
            var list = new List<long> { 1L, 2L, 3L, 4L, 5L };
            var size = _sizer.GetSizeBytes(list);
            
            Assert.That(size, Is.GreaterThan(40)); // at least 8 * 5 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ListOfString_UsesPointerSize()
        {
            var list = new List<string> { "one", "two", "three" };
            var size = _sizer.GetSizeBytes(list);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_ListOfDouble_ScalesWithCount()
        {
            var list = new List<double> { 1.1, 2.2, 3.3, 4.4, 5.5 };
            var size = _sizer.GetSizeBytes(list);
            
            Assert.That(size, Is.GreaterThan(40)); // at least 8 * 5 + overhead
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        #endregion

        #region Unknown/Fallback Types

        [Test]
        public void GetSizeBytes_CustomClass_ReturnsFallbackSize()
        {
            var obj = new TestClass { Value = 42 };
            var size = _sizer.GetSizeBytes(obj);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
            Assert.That(size, Is.EqualTo(128), "Should return conservative fallback estimate");
        }

        [Test]
        public void GetSizeBytes_AnonymousType_ReturnsFallbackSize()
        {
            var obj = new { A = 1, B = "test", C = 3.14 };
            var size = _sizer.GetSizeBytes(obj);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_Dictionary_ReturnsFallbackSize()
        {
            var dict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 };
            var size = _sizer.GetSizeBytes(dict);
            
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetSizeBytes_VeryLargeArray_HandlesLargeSize()
        {
            var arr = new byte[1024 * 1024]; // 1 MB
            var size = _sizer.GetSizeBytes(arr);
            
            Assert.That(size, Is.GreaterThan(1024 * 1024));
            Assert.That(size % 8, Is.EqualTo(0), "Size should be 8-byte aligned");
        }

        [Test]
        public void GetSizeBytes_MultipleCallsSameValue_ReturnsConsistentResult()
        {
            var value = "test string";
            var size1 = _sizer.GetSizeBytes(value);
            var size2 = _sizer.GetSizeBytes(value);
            var size3 = _sizer.GetSizeBytes(value);
            
            Assert.That(size2, Is.EqualTo(size1));
            Assert.That(size3, Is.EqualTo(size1));
        }

        [Test]
        public void GetSizeBytes_MultipleDifferentTypes_AllAligned()
        {
            var values = new object[]
            {
                42,
                "test",
                new byte[10],
                3.14,
                DateTime.Now,
                Guid.NewGuid(),
                new int[5]
            };

            foreach (var value in values)
            {
                var size = _sizer.GetSizeBytes(value);
                Assert.That(size % 8, Is.EqualTo(0), $"Size for {value.GetType().Name} should be 8-byte aligned");
            }
        }

        #endregion

        #region Helper Classes

        private class TestClass
        {
            public int Value { get; set; }
        }

        #endregion
    }
}
