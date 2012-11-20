using System;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
namespace MvcIntegrationTestFramework.Interception
{
    internal static class LastRequestData
    {
        public static ActionExecutedContext ActionExecutedContext
        {
            get;
            set;
        }
        public static ResultExecutedContext ResultExecutedContext
        {
            get;
            set;
        }
        public static HttpSessionState HttpSessionState
        {
            get;
            set;
        }
        public static HttpResponse Response
        {
            get;
            set;
        }
        public static void Reset()
        {
            LastRequestData.ActionExecutedContext = null;
            LastRequestData.ResultExecutedContext = null;
            LastRequestData.HttpSessionState = null;
            LastRequestData.Response = null;
        }
    }
}
