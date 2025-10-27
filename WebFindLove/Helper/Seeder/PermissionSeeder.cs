using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using WebFindLove.Models;
using WebFindLove.Models.Entities;

namespace WebFindLove.Helper.Seeder
{
    public static class PermissionSeeder
    {
        public static void SyncPermissions(AppDbContext context)
        {
            try
            {
                Console.WriteLine("🔍 Bắt đầu quét Controllers để đồng bộ Permissions...");

                var controllerTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                Console.WriteLine($"   Tìm thấy {controllerTypes.Count} Controllers");

                var newPermissions = new List<Permission>();
                var existingPermissions = context.Permissions.Select(p => p.Name).ToHashSet();

                foreach (var controller in controllerTypes)
                {
                    var moduleName = controller.Name.Replace("Controller", "");

                    var actions = controller
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m =>
                        {
                            var returnType = m.ReturnType;
                            
                            // Kiểm tra ActionResult và IActionResult
                            if (typeof(IActionResult).IsAssignableFrom(returnType))
                                return true;
                            
                            // Kiểm tra Task<ActionResult> và Task<IActionResult>
                            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                            {
                                var taskResultType = returnType.GetGenericArguments()[0];
                                return typeof(IActionResult).IsAssignableFrom(taskResultType);
                            }
                            
                            return false;
                        })
                        .Select(m => m.Name)
                        .Distinct()
                        .ToList();

                    foreach (var action in actions)
                    {
                        var permName = $"{moduleName}.{action}";
                        
                        // Kiểm tra trong HashSet thay vì query DB mỗi lần
                        if (!existingPermissions.Contains(permName))
                        {
                            newPermissions.Add(new Permission
                            {
                                Id = Guid.NewGuid(),
                                Module = moduleName,
                                Action = action,
                                Name = permName,
                                Description = $"Quyền {permName}",
                                IsActive = true
                            });
                        }
                    }

                    Console.WriteLine($"   ✓ {moduleName}: {actions.Count} actions");
                }

                if (newPermissions.Count > 0)
                {
                    context.Permissions.AddRange(newPermissions);
                    context.SaveChanges();
                    Console.WriteLine($"✅ Đã thêm {newPermissions.Count} quyền mới vào bảng Permission.");
                    
                    // Hiển thị danh sách permissions mới
                    foreach (var perm in newPermissions)
                    {
                        Console.WriteLine($"   + {perm.Name}");
                    }
                }
                else
                {
                    Console.WriteLine("✅ Không có quyền mới nào cần thêm. Database đã đồng bộ.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi đồng bộ Permissions: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
