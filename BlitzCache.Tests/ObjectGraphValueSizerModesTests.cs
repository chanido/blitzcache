using BlitzCacheCore.Statistics.Memory;
using NUnit.Framework;
using System.Collections.Generic;

namespace BlitzCacheCore.Tests
{
    public class ObjectGraphValueSizerModesTests
    {
        private class Complex
        {
            public string Name = new string('x', 32);
            public List<int> Numbers = new List<int>(new int[50]);
            public Complex Child;
        }

        [Test]
        public void FastMode_Is_NotZero()
        {
            var obj = new Complex();
            var fast = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { Mode = SizeComputationMode.Fast });
            var size = fast.GetSizeBytes(obj);
            Assert.That(size, Is.GreaterThan(0));
        }

        [Test]
        public void Mode_Relative_Order()
        {
            var obj = new Complex { Child = new Complex() };
            var fast = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { Mode = SizeComputationMode.Fast });
            var balanced = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { Mode = SizeComputationMode.Balanced });
            var accurate = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { Mode = SizeComputationMode.Accurate });
            var adaptive = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { Mode = SizeComputationMode.Adaptive });

            long f = fast.GetSizeBytes(obj);
            long b = balanced.GetSizeBytes(obj);
            long ad = adaptive.GetSizeBytes(obj);
            long a = accurate.GetSizeBytes(obj);

            Assert.That(f, Is.LessThanOrEqualTo(b), "Fast should be <= Balanced");
            Assert.That(f, Is.LessThanOrEqualTo(ad), "Fast should be <= Adaptive");
            Assert.That(b, Is.LessThanOrEqualTo(a), "Balanced should be <= Accurate");
            Assert.That(ad, Is.LessThanOrEqualTo(a), "Adaptive should be <= Accurate");
        }

        [Test]
        public void Adaptive_NotZero()
        {
            var obj = new Complex();
            var adaptive = new ObjectGraphValueSizer(new ObjectGraphSizerOptions { Mode = SizeComputationMode.Adaptive });
            Assert.That(adaptive.GetSizeBytes(obj), Is.GreaterThan(0));
        }
    }
}
