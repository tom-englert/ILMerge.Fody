namespace Tests
{
    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    using Xunit;

    public class UnitTest1
    {
        private AutoWeakIndexer<string, string> _indexer = new AutoWeakIndexer<string, string>(a => "x" + a);
        private DispatcherThrottle _throttle;

        [Fact]
        public void Test1()
        {
            _throttle = new DispatcherThrottle(Test1);

            Assert.True(_indexer.GetType().Assembly == typeof(UnitTest1).Assembly);
            Assert.True(_throttle.GetType().Assembly == typeof(UnitTest1).Assembly);
        }
    }
}
