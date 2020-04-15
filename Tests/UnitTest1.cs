// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedVariable
[assembly: ILMerge.IncludeAssemblies("TomsToolbox")]
[assembly: ILMerge.HideImportedTypes(false)]

namespace Tests
{
    using System;

    using TomsToolbox.Core;
    using Xunit;

    public class UnitTest1
    {
        private readonly AutoWeakIndexer<string, string> _indexer = new AutoWeakIndexer<string, string>(a => "x" + a);

        [Fact]
        public void Test()
        {
            Assert.True(_indexer.GetType().Assembly == typeof(UnitTest1).Assembly);
        }
    }

    public class UnitTest2
    {
        private readonly Type _t = typeof(SomeComplexSample<DelegateComparer<TomsToolbox.Core.ITimeService>, TomsToolbox.Core.ITimeService>);

        [Fact]
        public void Test()
        {
            Assert.True(_t.Assembly == typeof(UnitTest1).Assembly);
        }
    }

#if NETFRAMEWORK
    namespace NetFrameworkOnly
    {
        using TomsToolbox.Desktop;

        public class UnitTest3
        {
            private DispatcherThrottle _throttle;

            [Fact]
            public void Test1()
            {
                _throttle = new DispatcherThrottle(Test1);
                Assert.True(_throttle.GetType().Assembly == typeof(UnitTest1).Assembly);
            }
        }
    }
#endif

    // some complex class, just make sure this can be handled ...
    class SomeComplexSample<T1, T2> : TomsToolbox.Core.WeakEventListener<T1, T2, EventArgs>
        where T1 : TomsToolbox.Core.DelegateComparer<T2>
        where T2 : class, TomsToolbox.Core.ITimeService
    {
        public SomeComplexSample(T1 target, T2 source, Action<T1, object, EventArgs> onEventAction, Action<WeakEventListener<T1, T2, EventArgs>, T2> onAttachAction, Action<WeakEventListener<T1, T2, EventArgs>, T2> onDetachAction)
            : base(target, source, onEventAction, onAttachAction, onDetachAction)
        {
        }

        public SomeComplexSample(T1 target, TomsToolbox.Core.WeakReference<T2> source, Action<T1, object, EventArgs> onEventAction, Action<WeakEventListener<T1, T2, EventArgs>, T2> onAttachAction, Action<WeakEventListener<T1, T2, EventArgs>, T2> onDetachAction)
            : base(target, source, onEventAction, onAttachAction, onDetachAction)
        {
        }

        public T SomeMethod<T>(TomsToolbox.Core.TryCastWorker<T> p1)
            where T : TomsToolbox.Core.DelegateComparer<AutoWeakIndexer<int, string>>
        {
            var x = new AutoWeakIndexer<int, string>(i => i.ToString());

            var comparer = x.Comparer;
            var keys = x.Keys;

            if (comparer != null && keys.IsReadOnly)
            {
                throw new Exception("never happens");
            }

            return default;
        }

        public void AnotherMethod()
        {
            var y = default(TomsToolbox.Core.DelegateComparer<AutoWeakIndexer<int, string>>).TryCast();

            var x = SomeMethod(y);
        }
    }
}
