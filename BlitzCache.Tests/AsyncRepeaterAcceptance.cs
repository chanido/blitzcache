using BlitzCache.Tests.Helpers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace BlitzCache.Tests
{
    public class AsyncRepeaterAcceptance
    {
        private const int nomberOfTests = 50;
        [Test]
        public async Task ShouldSendsTasksInParallel()
        {
            var slowClass = new SlowClassAsync();

            await AsyncRepeater.Go(nomberOfTests, async () => await slowClass.ProcessSlowly());

            Assert.AreEqual(nomberOfTests, slowClass.Counter);
        }
    }
}
