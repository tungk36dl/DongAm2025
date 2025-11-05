using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using WebFindLove.Models.Services.RolePermissionService;

namespace WebFindLove.Helper.Authorization
{
	public class RoutePermissionMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<RoutePermissionMiddleware> _logger;

		public RoutePermissionMiddleware(RequestDelegate next, ILogger<RoutePermissionMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context, IRolePermissionService rolePermissionService)
		{
			// Skip for non-endpoint requests (e.g., static files) or if endpoint not resolved yet
			var endpoint = context.GetEndpoint();
			if (endpoint == null)
			{
				await _next(context);
				return;
			}

			// Skip if the endpoint allows anonymous access
			if (endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null)
			{
				await _next(context);
				return;
			}

			// Require authenticated user (let the standard auth middleware handle challenges)
			if (!(context.User.Identity?.IsAuthenticated ?? false))
			{
				await _next(context);
				return;
			}

			// Admin bypass
			var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;
			if (!string.IsNullOrWhiteSpace(roleName) && string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
			{
				await _next(context);
				return;
			}

			// Build permission key from route data: {Controller}.{Action}
			var routeData = context.GetRouteData();
			var controller = routeData?.Values["controller"]?.ToString();
			var action = routeData?.Values["action"]?.ToString();

			if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
			{
				await _next(context);
				return;
			}

			var permissionKey = $"{controller}.{action}";

			// Extract userId from claims
			var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (!Guid.TryParse(userIdClaim, out var userId))
			{
				await _next(context);
				return;
			}

			// Check permission against role-permission store
			var checkResult = await rolePermissionService.CheckUserPermissionAsync(userId, permissionKey);
			if (!checkResult.Success)
			{
				_logger.LogWarning("Permission check error for user {UserId} on {Permission}: {Message}", userId, permissionKey, checkResult.Message);
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				await context.Response.WriteAsync("Permission check failed");
				return;
			}

			if (!checkResult.Data)
			{
				_logger.LogInformation("User {UserId} denied access to {Permission}", userId, permissionKey);

				// AJAX request handling
				if (IsAjaxRequest(context.Request))
				{
					context.Response.StatusCode = StatusCodes.Status403Forbidden;
					context.Response.ContentType = "application/json";
					await context.Response.WriteAsync("{\"success\":false,\"message\":\"Bạn không có quyền truy cập chức năng này\",\"errorCode\":\"PERMISSION_DENIED\"}");
					return;
				}

				// Non-AJAX: redirect to a friendly AccessDenied page
				context.Response.Redirect("/Error/AccessDenied");
				return;
			}

			await _next(context);
		}

		private static bool IsAjaxRequest(HttpRequest request)
		{
			return request.Headers.ContainsKey("X-Requested-With") && request.Headers["X-Requested-With"] == "XMLHttpRequest";
		}
	}
}


