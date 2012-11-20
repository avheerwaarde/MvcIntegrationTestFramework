using MvcIntegrationTestFramework.Browsing;
using System;
namespace MvcIntegrationTestFramework.Hosting
{
    internal class AppDomainProxy : MarshalByRefObject
    {
        public void RunCodeInAppDomain(Action codeToRun)
        {
            codeToRun();
        }
        public void RunBrowsingSessionInAppDomain(SerializableDelegate<Action<BrowsingSession>> script)
        {
            BrowsingSession instance = BrowsingSession.Instance;
            script.Delegate(instance);
        }
        public FuncExecutionResult<TResult> RunBrowsingSessionInAppDomain<TResult>(SerializableDelegate<Func<BrowsingSession, TResult>> script)
        {
            BrowsingSession instance = BrowsingSession.Instance;
            TResult delegateCallResult = script.Delegate(instance);
            return new FuncExecutionResult<TResult>
            {
                DelegateCalled = script,
                DelegateCallResult = delegateCallResult
            };
        }
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
