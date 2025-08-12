using System;
using System.Collections.Generic;
using System.Diagnostics;
using BlitzCacheCore.Statistics.Memory;
using NUnit.Framework;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class ObjectGraphValueSizerBenchmarkTests
    {
        private readonly IValueSizer approx = new ApproximateValueSizer();
        private readonly IValueSizer graph = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { MaxDepth = 2, MaxObjects = 512 });

        private class Leaf { public int A; public long B; public string C = new string('x', 16); }
        private class Node { public string Name = "node"; public List<Leaf> Leaves = new List<Leaf>(); public Node Next; }

        private object BuildGraph(int depth, int breadth)
        {
            Node root = new Node();
            Node current = root;
            for (int d = 0; d < depth; d++)
            {
                for (int i = 0; i < breadth; i++) current.Leaves.Add(new Leaf());
                if (d < depth - 1)
                {
                    current.Next = new Node();
                    current = current.Next;
                }
            }
            return root;
        }

        private long RunSizer(IValueSizer sizer, object value, int iterations)
        {
            long checksum = 0;
            for (int i = 0; i < iterations; i++) checksum += sizer.GetSizeBytes(value);
            return checksum;
        }

        private (double approxNs, double graphNs) Measure(object value, int iterations)
        {
            // Warmup
            RunSizer(approx, value, 10);
            RunSizer(graph, value, 10);

            var sw = Stopwatch.StartNew();
            RunSizer(approx, value, iterations);
            sw.Stop();
            double approxPer = (sw.Elapsed.TotalMilliseconds * 1_000_000.0) / iterations; // ns

            sw.Restart();
            RunSizer(graph, value, iterations);
            sw.Stop();
            double graphPer = (sw.Elapsed.TotalMilliseconds * 1_000_000.0) / iterations;

            return (approxPer, graphPer);
        }

        [Test, Explicit("Micro-benchmark; run manually")] 
        public void Compare_Primitive_String()
        {
            var (a, g) = Measure(new string('x', 64), 20000);
            TestContext.WriteLine($"String 64 chars: approx ~{a:F1} ns vs graph ~{g:F1} ns (x{(g/a):F2})");
        }

        [Test, Explicit("Micro-benchmark; run manually")] 
        public void Compare_Array_Int()
        {
            var (a, g) = Measure(new int[256], 10000);
            TestContext.WriteLine($"int[256]: approx ~{a:F1} ns vs graph ~{g:F1} ns (x{(g/a):F2})");
        }

        [Test, Explicit("Micro-benchmark; run manually")] 
        public void Compare_ObjectGraph_Shallow()
        {
            var obj = new { A=1, B=2L, C="hello", D=DateTime.UtcNow };
            var (a, g) = Measure(obj, 20000);
            TestContext.WriteLine($"Anon shallow: approx ~{a:F1} ns vs graph ~{g:F1} ns (x{(g/a):F2})");
        }

        [Test, Explicit("Micro-benchmark; run manually")] 
        public void Compare_ObjectGraph_Deep()
        {
            var graphObj = BuildGraph(depth:4, breadth:8); // up to depth 4, but sizer limited to 2
            var (a, g) = Measure(graphObj, 2000);
            TestContext.WriteLine($"Node graph(depth4,breadth8): approx ~{a:F1} ns vs graph ~{g:F1} ns (x{(g/a):F2})");
        }
    }
}
