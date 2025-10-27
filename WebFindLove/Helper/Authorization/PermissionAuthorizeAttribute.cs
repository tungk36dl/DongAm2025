using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace WebFindLove.Helper.Authorization
{
    /// <summary>
    /// Attribute để kiểm tra quyền truy cập dựa trên Permission
    /// Sử dụng: [PermissionAuthorize("Users.Edit")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PermissionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _requiredPermissions;
        private readonly bool _requireAll;

        /// <summary>
        /// Khởi tạo attribute với các quyền cần thiết
        /// </summary>
        /// <param name="permissions">Danh sách quyền cần thiết</param>
        public PermissionAuthorizeAttribute(params string[] permissions)
        {
            _requiredPermissions = permissions ?? Array.Empty<string>();
            _requireAll = false; // Mặc định chỉ cần 1 quyền trong danh sách
        }

        /// <summary>
        /// Khởi tạo attribute với các quyền cần thiết và tùy chọn yêu cầu tất cả
        /// </summary>
        /// <param name="requireAll">True: Yêu cầu có tất cả quyền. False: Chỉ cần 1 quyền</param>
        /// <param name="permissions">Danh sách quyền cần thiết</param>
        public PermissionAuthorizeAttribute(bool requireAll, params string[] permissions)
        {
            _requiredPermissions = permissions ?? Array.Empty<string>();
            _requireAll = requireAll;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Kiểm tra user đã đăng nhập chưa
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Nếu không yêu cầu quyền cụ thể, cho phép truy cập
            if (_requiredPermissions.Length == 0)
            {
                return;
            }

            // Lấy tất cả permissions từ claims
            var userPermissions = user.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Kiểm tra quyền Admin (có toàn quyền)
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Admin có tất cả quyền
                return;
            }

            // Kiểm tra quyền
            bool hasPermission;
            if (_requireAll)
            {
                // Yêu cầu có tất cả quyền
                hasPermission = _requiredPermissions.All(p => userPermissions.Contains(p));
            }
            else
            {
                // Chỉ cần có 1 quyền trong danh sách
                hasPermission = _requiredPermissions.Any(p => userPermissions.Contains(p));
            }

            if (!hasPermission)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<PermissionAuthorizeAttribute>>();
                logger?.LogWarning(
                    "User {Username} (ID: {UserId}) attempted to access {Controller}.{Action} without required permissions: {Permissions}",
                    user.Identity?.Name,
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    context.RouteData.Values["controller"],
                    context.RouteData.Values["action"],
                    string.Join(", ", _requiredPermissions));

                // Trả về 403 Forbidden hoặc redirect tùy theo request type
                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = "Bạn không có quyền truy cập chức năng này",
                        errorCode = "PERMISSION_DENIED"
                    })
                    {
                        StatusCode = 403
                    };
                }
                else
                {
                    context.Result = new ViewResult
                    {
                        ViewName = "~/Views/Shared/AccessDenied.cshtml",
                        StatusCode = 403,
                        ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                        {
                            ["RequiredPermissions"] = string.Join(", ", _requiredPermissions),
                            ["UserPermissions"] = string.Join(", ", userPermissions)
                        }
                    };
                }
            }
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers.ContainsKey("X-Requested-With") &&
                   request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}

