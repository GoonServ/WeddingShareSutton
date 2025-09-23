using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WeddingShare.Enums;
using WeddingShare.Extensions;
using WeddingShare.Helpers;

namespace WeddingShare.Attributes
{
    public class RequiresRoleAttribute : ActionFilterAttribute
    {
        public UserLevel User { get; set; } = UserLevel.Basic;
        public AccessPermissions Permission { get; set; } = AccessPermissions.Login;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var level = filterContext.HttpContext?.User?.Identity?.GetUserLevel() ?? UserLevel.Basic;
                if (level < this.User)
                {
                    filterContext.Result = new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.Unauthorized }, false);
                }
 
                var pemissions = filterContext.HttpContext?.User?.Identity?.GetUserPermissions() ?? AccessPermissions.None;
                if (!pemissions.HasFlag(this.Permission))
                {
                    filterContext.Result = new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.Unauthorized }, false);
                }
            }
            catch (Exception ex)
            {
                var logger = filterContext.HttpContext.RequestServices.GetService<ILogger<RequiresSecretKeyAttribute>>();
                if (logger != null)
                {
                    logger.LogError(ex, $"Failed to validate user role - {ex?.Message}");
                }
            }
        }
    }
}