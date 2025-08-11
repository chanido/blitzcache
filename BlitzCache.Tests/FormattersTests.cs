using BlitzCacheCore;
using NUnit.Framework;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class FormattersTests
    {
        [TestCase(0L, "0ms")]
        [TestCase(1L, "1ms")]
        [TestCase(999L, "999ms")]
        [TestCase(1000L, "1.000s")]
        [TestCase(1500L, "1.500s")]
        [TestCase(59_999L, "59.999s")]
        [TestCase(60_000L, "00:01:00")]
        [TestCase(61_000L, "00:01:01")]
        [TestCase(61_234L, "00:01:01")]
        [TestCase(3_600_000L, "01:00:00")]
        [TestCase(3_661_000L, "01:01:01")]
        [TestCase(7_200_000L, "02:00:00")]
        [TestCase(86_400_000L, "1d 00:00:00")]
        [TestCase(86_461_000L, "1d 00:01:01")]
        public void FormatDuration_Produces_Expected_Output(long inputMs, string expected)
        {
            var actual = Formatters.FormatDuration(inputMs);
            Assert.AreEqual(expected, actual);
        }

        [TestCase(0L, "0 bytes")]
        [TestCase(1L, "1 bytes")]
        [TestCase(1023L, "1023 bytes")]
        [TestCase(1024L, "1 KB")]
        [TestCase(1536L, "1.5 KB")]
        [TestCase(1100L, "1.07 KB")]
        [TestCase(1_048_576L, "1 MB")]
        [TestCase(1_572_864L, "1.5 MB")]
        [TestCase(2_621_440L, "2.5 MB")]
        [TestCase(1_073_741_824L, "1 GB")]
        [TestCase(1_610_612_736L, "1.5 GB")]
        [TestCase(2_684_354_560L, "2.5 GB")]
        public void FormatBytes_Produces_Expected_Output(long inputBytes, string expected)
        {
            var actual = Formatters.FormatBytes(inputBytes);
            Assert.AreEqual(expected, actual);
        }
    }
}
