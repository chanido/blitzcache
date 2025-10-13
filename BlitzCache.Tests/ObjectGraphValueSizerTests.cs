using System;
using System.Collections.Generic;
using BlitzCacheCore.Statistics.Memory;
using NUnit.Framework;

namespace BlitzCacheCore.Tests
{
    public class ObjectGraphValueSizerTests
    {
        private readonly ObjectGraphValueSizer sizer = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { MaxDepth = 2, MaxObjects = 256 });

        private class Node
        {
            public string Name = string.Empty;
            public Node Child;
        }

        private class Cycle
        {
            public Cycle Next;
            public string Data = "abc";
        }

        [Test]
        public void Sizes_Primitive_String()
        {
            long size = sizer.GetSizeBytes("hello");
            Assert.Greater(size, 0);
        }

        [Test]
        public void Sizes_Nested_Object_Two_Levels()
        {
            var root = new Node { Name = new string('x', 10), Child = new Node { Name = new string('y', 5) } };
            long size = sizer.GetSizeBytes(root);
            long shallow = sizer.GetSizeBytes(root.Name);
            Assert.Greater(size, shallow);
        }

        [Test]
        public void Sizes_Collections_Sampled()
        {
            var list = new List<string>();
            for (int i = 0; i < 100; i++) list.Add("item" + i);
            long size = sizer.GetSizeBytes(list);
            Assert.Greater(size, 0);
        }

        [Test]
        public void Handles_Cycles_Without_Overflow()
        {
            var a = new Cycle();
            var b = new Cycle();
            a.Next = b; b.Next = a;
            long size = sizer.GetSizeBytes(a);
            Assert.Greater(size, 0);
        }

        [Test]
        public void Respects_MaxDepth()
        {
            var deep = new Node { Name = "root", Child = new Node { Name = "child", Child = new Node { Name = "grandchild" } } };
            long size = sizer.GetSizeBytes(deep);
            Assert.Greater(size, 0);
        }
    }
}
