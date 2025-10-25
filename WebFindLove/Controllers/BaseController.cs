using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WebFindLove.Models;
using WebFindLove.Models.Services;
using WebFindLove.Helper;

namespace WebFindLove.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
        protected ILogger Logger { get; set; } = null!;
        protected User? CurrentUser
        {
            get
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return new User
                        {
                            Id = userId,
                            UserName = User.FindFirst(ClaimTypes.Name)?.Value,
                            Email = User.FindFirst(ClaimTypes.Email)?.Value,
                            RoleName = User.FindFirst(ClaimTypes.Role)?.Value
                        };
                    }
                }
                return null;
            }
        }

        protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        protected string? UserRole => User.FindFirst(ClaimTypes.Role)?.Value;

        protected Guid? UserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
            }
        }

        /// <summary>
        /// Handle service response and return appropriate view or redirect
        /// </summary>
        protected IActionResult HandleServiceResponse<T>(DataResponse<T> response, string successAction = "Index", object? routeValues = null)
        {
            if (response.Success)
            {
                Logger?.LogDebug("Service response successful - Action: {Action}", successAction);
                if (response.Data != null)
                    return View(response.Data);
                return RedirectToAction(successAction, routeValues);
            }

            Logger?.LogWarning("Service response failed - Message: {Message}, ErrorDetails: {ErrorDetails}", 
                response.Message, response.ErrorDetails);

            ModelState.AddDataResponse(new DataResponse<object> 
            { 
                Success = response.Success, 
                Message = response.Message, 
                ErrorDetails = response.ErrorDetails 
            });
            return View();
        }

        /// <summary>
        /// Handle service response for API calls
        /// </summary>
        protected IActionResult HandleApiResponse<T>(DataResponse<T> response)
        {
            if (response.Success)
            {
                Logger?.LogDebug("API response successful - Data type: {DataType}", typeof(T).Name);
                return Json(new { success = true, data = response.Data });
            }

            Logger?.LogWarning("API response failed - Message: {Message}, ErrorDetails: {ErrorDetails}", 
                response.Message, response.ErrorDetails);

            return Json(new { 
                success = false, 
                message = response.Message,
                errorDetails = response.ErrorDetails
            });
        }
    }
}
