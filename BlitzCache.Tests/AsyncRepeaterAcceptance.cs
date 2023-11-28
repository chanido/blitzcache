using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    public class AsyncRepeaterAcceptance
    {
        private const int numberOfTests = 50;
        [Test]
        public async Task ShouldSendsTasksInParallel()
        {
            var slowClass = new SlowClassAsync();

            await AsyncRepeater.Go(numberOfTests, async () => await slowClass.ProcessSlowly());

            Assert.AreEqual(numberOfTests, slowClass.Counter);
        }
    }
}
