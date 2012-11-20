using System;
using System.Web;
using System.Web.Mvc;
namespace MvcIntegrationTestFramework.Interception
{
    internal class InterceptionFilter : ActionFilterAttribute
    {
        private HttpContext lastHttpContext;
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (this.lastHttpContext == null)
            {
                this.lastHttpContext = HttpContext.Current;
            }
            if (filterContext != null && LastRequestData.ActionExecutedContext == null)
            {
                LastRequestData.ActionExecutedContext = new ActionExecutedContext
                {
                    ActionDescriptor = filterContext.ActionDescriptor,
                    Canceled = filterContext.Canceled,
                    Controller = filterContext.Controller,
                    Exception = filterContext.Exception,
                    ExceptionHandled = filterContext.ExceptionHandled,
                    HttpContext = filterContext.HttpContext,
                    RequestContext = filterContext.RequestContext,
                    Result = filterContext.Result,
                    RouteData = filterContext.RouteData
                };
            }
        }
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext != null && LastRequestData.ResultExecutedContext == null)
            {
                LastRequestData.ResultExecutedContext = new ResultExecutedContext
                {
                    Canceled = filterContext.Canceled,
                    Exception = filterContext.Exception,
                    Controller = filterContext.Controller,
                    ExceptionHandled = filterContext.ExceptionHandled,
                    HttpContext = filterContext.HttpContext,
                    RequestContext = filterContext.RequestContext,
                    Result = filterContext.Result,
                    RouteData = filterContext.RouteData
                };
            }
        }
    }
}
