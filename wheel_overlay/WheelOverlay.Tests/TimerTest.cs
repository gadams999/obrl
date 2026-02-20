using System;
using System.Threading;
using Xunit;

namespace WheelOverlay.Tests
{
    public class TimerTest
    {
        [Fact]
        public void SystemThreadingTimer_ShouldFire()
        {
            int callCount = 0;
            using (var timer = new System.Threading.Timer(_ =>
            {
                Interlocked.Increment(ref callCount);
            }, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)))
            {
                // Wait longer to ensure timer fires at least once
                Thread.Sleep(1000);

                Assert.True(callCount > 0, $"Timer should have fired at least once, but callCount={callCount}");
            }
        }
    }
}
