using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.API.Filters;

public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public RequireRoleAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var role = context.HttpContext.Session.GetString("Role");

        if (userId == null || role == null)
        {
            context.Result = new UnauthorizedObjectResult("You must be logged in to access this resource.");
            return;
        }

        if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(role))
        {
            context.Result = new ObjectResult("You do not have permission to access this resource.")
            {
                StatusCode = 403
            };
        }
    }
}