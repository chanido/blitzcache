using BlitzCache.Tests.Helpers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace BlitzCache.Tests
{
    public class AsyncRepeaterAcceptance
    {
        [Test]
        public async Task ShouldSendsTasksInParallel()
        {
            var slowClass = new SlowClassAsync();

            await AsyncRepeater.Go(50, async () => await slowClass.ProcessSlowly());

            Assert.IsTrue(slowClass.Counter > 1);
        }
    }
}
