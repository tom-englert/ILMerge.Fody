[assembly: ILMerge.IncludeAssemblies("TomsToolbox")]

namespace Tests
{
    using System;

    using JetBrains.Annotations;

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

#if NETFRAMEWORK
    namespace NetFrameworkOnly
    {
        using System.Reflection;

        using TomsToolbox.Desktop;

        public class UnitTest2
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

    class Reference<T1, T2> : TomsToolbox.Core.WeakEventListener<T1, T2, EventArgs>
        where T1 : TomsToolbox.Core.DelegateComparer<T2>
        where T2 : class, TomsToolbox.Core.ITimeService
    {
        public Reference([NotNull] T1 target, [NotNull] T2 source, [NotNull] Action<T1, object, EventArgs> onEventAction, [NotNull] Action<WeakEventListener<T1, T2, EventArgs>, T2> onAttachAction, [NotNull] Action<WeakEventListener<T1, T2, EventArgs>, T2> onDetachAction)
            : base(target, source, onEventAction, onAttachAction, onDetachAction)
        {
        }

        public Reference([NotNull] T1 target, [NotNull] TomsToolbox.Core.WeakReference<T2> source, [NotNull] Action<T1, object, EventArgs> onEventAction, [NotNull] Action<WeakEventListener<T1, T2, EventArgs>, T2> onAttachAction, [NotNull] Action<WeakEventListener<T1, T2, EventArgs>, T2> onDetachAction) 
            : base(target, source, onEventAction, onAttachAction, onDetachAction)
        {
        }

        public T SomeMethod<T>(TomsToolbox.Core.TryCastWorker<T> p1) 
            where T : TomsToolbox.Core.DelegateComparer<AutoWeakIndexer<int, string>>
        {
            return default(T);
        }

        public void AnotherMethod()
        {
            var y = default(TomsToolbox.Core.DelegateComparer<AutoWeakIndexer<int, string>>).TryCast();

            var x = SomeMethod(y);
        }
    }
}
