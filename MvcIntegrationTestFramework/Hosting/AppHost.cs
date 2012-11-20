using MvcIntegrationTestFramework.Browsing;
using MvcIntegrationTestFramework.Interception;
using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
namespace MvcIntegrationTestFramework.Hosting
{
    public class AppHost
    {
        private readonly AppDomainProxy _appDomainProxy;
        private static readonly MethodInfo getApplicationInstanceMethod;
        private static readonly MethodInfo recycleApplicationInstanceMethod;
        private AppHost(string appPhysicalDirectory, string virtualDirectory = "/")
        {
            this._appDomainProxy = (AppDomainProxy)ApplicationHost.CreateApplicationHost(typeof(AppDomainProxy), virtualDirectory, appPhysicalDirectory);
            this._appDomainProxy.RunCodeInAppDomain(delegate
            {
                AppHost.InitializeApplication();
                FilterProviders.Providers.Add(new InterceptionFilterProvider());
                LastRequestData.Reset();
            }
            );
        }
        public void Start(Action<BrowsingSession> testScript)
        {
            SerializableDelegate<Action<BrowsingSession>> script = new SerializableDelegate<Action<BrowsingSession>>(testScript);
            this._appDomainProxy.RunBrowsingSessionInAppDomain(script);
        }
        public TResult Start<TResult>(Func<BrowsingSession, TResult> testScript)
        {
            SerializableDelegate<Func<BrowsingSession, TResult>> script = new SerializableDelegate<Func<BrowsingSession, TResult>>(testScript);
            FuncExecutionResult<TResult> funcExecutionResult = this._appDomainProxy.RunBrowsingSessionInAppDomain<TResult>(script);
            AppHost.CopyFields<object>(funcExecutionResult.DelegateCalled.Delegate.Target, testScript.Target);
            return funcExecutionResult.DelegateCallResult;
        }
        private static void CopyFields<T>(T from, T to) where T : class
        {
            if (from != null && to != null)
            {
                FieldInfo[] fields = from.GetType().GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo fieldInfo = fields[i];
                    fieldInfo.SetValue(to, fieldInfo.GetValue(from));
                }
            }
        }
        private static void InitializeApplication()
        {
            HttpApplication applicationInstance = AppHost.GetApplicationInstance();
            applicationInstance.PostRequestHandlerExecute += delegate(object param0, EventArgs param1)
            {
                if (LastRequestData.HttpSessionState == null)
                {
                    LastRequestData.HttpSessionState = HttpContext.Current.Session;
                }
                if (LastRequestData.Response == null)
                {
                    LastRequestData.Response = HttpContext.Current.Response;
                }
            }
            ;
            AppHost.RefreshEventsList(applicationInstance);
            AppHost.RecycleApplicationInstance(applicationInstance);
        }
        static AppHost()
        {
            Type type = typeof(HttpContext).Assembly.GetType("System.Web.HttpApplicationFactory", true);
            AppHost.getApplicationInstanceMethod = type.GetMethod("GetApplicationInstance", BindingFlags.Static | BindingFlags.NonPublic);
            AppHost.recycleApplicationInstanceMethod = type.GetMethod("RecycleApplicationInstance", BindingFlags.Static | BindingFlags.NonPublic);
        }
        private static HttpApplication GetApplicationInstance()
        {
            StringWriter output = new StringWriter();
            SimpleWorkerRequest wr = new SimpleWorkerRequest("", "", output);
            HttpContext httpContext = new HttpContext(wr);
            return (HttpApplication)AppHost.getApplicationInstanceMethod.Invoke(null, new object[]
			{
				httpContext
			});
        }
        private static void RecycleApplicationInstance(HttpApplication appInstance)
        {
            AppHost.recycleApplicationInstanceMethod.Invoke(null, new object[]
			{
				appInstance
			});
        }
        private static void RefreshEventsList(HttpApplication appInstance)
        {
            object value = typeof(HttpApplication).GetField("_stepManager", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(appInstance);
            object value2 = typeof(HttpApplication).GetField("_resumeStepsWaitCallback", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(appInstance);
            MethodInfo method = value.GetType().GetMethod("BuildSteps", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(value, new object[]
			{
				value2
			});
        }
        public static AppHost Simulate(string mvcProjectDirectory)
        {
            string mvcProjectPath = AppHost.GetMvcProjectPath(mvcProjectDirectory);
            if (mvcProjectPath == null)
            {
                throw new ArgumentException(string.Format("Mvc Project {0} not found", mvcProjectDirectory));
            }
            AppHost.CopyDllFiles(mvcProjectPath);
            return new AppHost(mvcProjectPath, "/");
        }
        private static void CopyDllFiles(string mvcProjectPath)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] files = Directory.GetFiles(baseDirectory, "*.dll");
            for (int i = 0; i < files.Length; i++)
            {
                string text = files[i];
                string text2 = Path.Combine(mvcProjectPath, "bin", Path.GetFileName(text));
                if (!File.Exists(text2) || File.GetCreationTimeUtc(text2) != File.GetCreationTimeUtc(text))
                {
                    File.Copy(text, text2, true);
                }
            }
        }
        private static string GetMvcProjectPath(string mvcProjectName)
        {
            string text = AppDomain.CurrentDomain.BaseDirectory;
            while (text.Contains("\\"))
            {
                text = text.Substring(0, text.LastIndexOf("\\"));
                string text2 = Path.Combine(text, mvcProjectName);
                if (Directory.Exists(text2))
                {
                    return text2;
                }
            }
            return null;
        }
    }
}