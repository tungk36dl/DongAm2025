# Controller & View Templates - WebFindLove

## ✅ Đã hoàn thành

### Controllers
- ✅ **UsersController** (Full CRUD)
- ✅ **RolesController** (Full CRUD)
- ✅ **AuthController** (Login, Register, Logout)
- ✅ **HomeController** (Index, Privacy, Error)
- ✅ **AdminController** (Dashboard)
- ✅ **PhotosController** (Full CRUD + SetPrimary)
- ✅ **MessagesController** (Conversations, Send)

### Views
- ✅ **Auth**: Login.cshtml, Register.cshtml
- ✅ **Users**: Index, Create, Edit, Details, Delete
- ✅ **Roles**: Index, Create, Edit, Details, Delete
- ✅ **Photos**: Index
- ✅ **Messages**: Index, Conversation

---

## 📋 Cần tạo thêm (Optional)

### 1. Photos Module - Remaining Views

#### Views/Photos/Create.cshtml
```cshtml
@model WebFindLove.Models.Services.PhotoService.ViewModels.PhotoCreateVM
@{
    ViewData["Title"] = "Add Photo";
}

<div class="container mx-auto px-4 py-8 max-w-2xl">
    <h1 class="text-3xl font-bold text-gray-800 mb-6">Add New Photo</h1>
    
    <div class="bg-white rounded-lg shadow-md p-6">
        <form asp-action="Create" method="post">
            @Html.AntiForgeryToken()
            
            <input type="hidden" asp-for="UserId" />
            
            <div class="mb-4">
                <label asp-for="PhotoUrl" class="block text-sm font-medium text-gray-700 mb-2"></label>
                <input asp-for="PhotoUrl" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500" />
                <span asp-validation-for="PhotoUrl" class="text-red-600 text-sm"></span>
            </div>
            
            <div class="mb-4">
                <label asp-for="Description" class="block text-sm font-medium text-gray-700 mb-2"></label>
                <textarea asp-for="Description" rows="3" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"></textarea>
                <span asp-validation-for="Description" class="text-red-600 text-sm"></span>
            </div>
            
            <div class="mb-6">
                <label class="flex items-center gap-2">
                    <input asp-for="IsPrimary" type="checkbox" class="rounded" />
                    <span class="text-sm font-medium text-gray-700">Set as primary photo</span>
                </label>
            </div>
            
            <div class="flex gap-3">
                <button type="submit" class="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg transition">
                    <i class="fas fa-save mr-2"></i>Save
                </button>
                <a asp-action="Index" class="bg-gray-500 hover:bg-gray-600 text-white px-6 py-2 rounded-lg transition">
                    Cancel
                </a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

#### Views/Photos/Edit.cshtml, Details.cshtml, Delete.cshtml
Tương tự như Users module, chỉ cần thay đổi model và fields tương ứng.

---

### 2. MatchResults Module

#### Controllers/MatchResultsController.cs
```csharp
[Authorize]
public class MatchResultsController : BaseController
{
    private readonly IMatchResultService _matchResultService;
    private readonly ILogger<MatchResultsController> _logger;

    public MatchResultsController(
        IMatchResultService matchResultService, 
        ILogger<MatchResultsController> logger)
    {
        _matchResultService = matchResultService;
        _logger = logger;
        Logger = logger;
        _logger.LogInformation("MatchResultsController initialized");
    }

    // GET: MatchResults - My matches
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("GET My Matches - User: {Username}", CurrentUser?.UserName);
        
        var response = await _matchResultService.GetMatchesByUserIdAsync(UserId!.Value);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to get matches: {Message}", response.Message);
            TempData["ErrorMessage"] = response.Message;
        }

        return View(response.Data ?? new List<MatchResult>());
    }

    // GET: MatchResults/TopMatches
    public async Task<IActionResult> TopMatches(int count = 10)
    {
        _logger.LogInformation("GET Top Matches - User: {Username}, Count: {Count}", 
            CurrentUser?.UserName, count);
        
        var response = await _matchResultService.GetTopMatchesAsync(UserId!.Value, count);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to get top matches: {Message}", response.Message);
            TempData["ErrorMessage"] = response.Message;
        }

        return View(response.Data ?? new List<MatchResult>());
    }

    // POST: MatchResults/Create (Admin only or from matching algorithm)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid userId1, Guid userId2, double? score, string? reasoning)
    {
        _logger.LogInformation("POST Create Match - User1: {User1}, User2: {User2}", 
            userId1, userId2);
        
        var response = await _matchResultService.CreateMatchAsync(userId1, userId2, score, reasoning);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to create match: {Message}", response.Message);
            TempData["ErrorMessage"] = response.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Match created successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: MatchResults/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("POST Delete Match - MatchId: {MatchId}", id);
        
        var response = await _matchResultService.DeleteMatchAsync(id);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to delete match: {Message}", response.Message);
            TempData["ErrorMessage"] = response.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Match removed successfully!";
        }

        return RedirectToAction(nameof(Index));
    }
}
```

#### Views/MatchResults/Index.cshtml
```cshtml
@model List<WebFindLove.Models.MatchResult>
@{
    ViewData["Title"] = "My Matches";
}

<div class="container mx-auto px-4 py-8">
    <h1 class="text-3xl font-bold text-gray-800 mb-6">My Matches</h1>
    
    @if (Model != null && Model.Any())
    {
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            @foreach (var match in Model.OrderByDescending(m => m.MatchScore))
            {
                var matchedUser = match.UserId.ToString() == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                    ? match.MatchedUser 
                    : match.User;
                
                <div class="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-xl transition">
                    <!-- Avatar -->
                    <div class="bg-gradient-to-br from-pink-500 to-purple-600 p-6 text-center">
                        <div class="w-24 h-24 bg-white rounded-full mx-auto flex items-center justify-center text-3xl font-bold text-purple-600 mb-4">
                            @(matchedUser?.UserName?.Substring(0, 1).ToUpper())
                        </div>
                        <h3 class="text-xl font-bold text-white">@matchedUser?.UserName</h3>
                    </div>
                    
                    <!-- Match Info -->
                    <div class="p-6">
                        <!-- Match Score -->
                        <div class="mb-4">
                            <div class="flex justify-between mb-2">
                                <span class="text-sm font-medium text-gray-700">Match Score</span>
                                <span class="text-sm font-bold text-purple-600">@match.MatchScore%</span>
                            </div>
                            <div class="w-full bg-gray-200 rounded-full h-3">
                                <div class="bg-gradient-to-r from-pink-500 to-purple-600 h-3 rounded-full" 
                                     style="width: @(match.MatchScore)%"></div>
                            </div>
                        </div>
                        
                        <!-- AI Reasoning -->
                        @if (!string.IsNullOrEmpty(match.AiReasoning))
                        {
                            <div class="mb-4 p-3 bg-purple-50 rounded-lg">
                                <p class="text-sm text-gray-700">
                                    <i class="fas fa-brain text-purple-600 mr-2"></i>
                                    @match.AiReasoning
                                </p>
                            </div>
                        }
                        
                        <!-- Actions -->
                        <div class="flex gap-2">
                            <a asp-controller="Messages" asp-action="Conversation" asp-route-userId="@matchedUser?.Id" 
                               class="flex-1 bg-blue-600 hover:bg-blue-700 text-white text-center py-2 rounded-lg transition">
                                <i class="fas fa-comment mr-1"></i>Message
                            </a>
                            <form asp-action="Delete" asp-route-id="@match.Id" method="post" class="flex-1">
                                @Html.AntiForgeryToken()
                                <button type="submit" class="w-full bg-red-500 hover:bg-red-600 text-white py-2 rounded-lg transition"
                                        onclick="return confirm('Remove this match?');">
                                    <i class="fas fa-times"></i>
                                </button>
                            </form>
                        </div>
                    </div>
                </div>
            }
        </div>
    }
    else
    {
        <div class="bg-white rounded-lg shadow-md p-12 text-center">
            <i class="fas fa-heart text-gray-400 text-6xl mb-4"></i>
            <h3 class="text-xl font-semibold text-gray-700 mb-2">No Matches Yet</h3>
            <p class="text-gray-500 mb-6">Complete your profile and preferences to find matches!</p>
            <a asp-controller="Users" asp-action="Edit" asp-route-id="@User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value" 
               class="inline-block bg-purple-600 hover:bg-purple-700 text-white px-6 py-3 rounded-lg transition">
                Complete Profile
            </a>
        </div>
    }
</div>
```

---

### 3. UserPreferences Module

#### Controllers/UserPreferencesController.cs
```csharp
[Authorize]
public class UserPreferencesController : BaseController
{
    private readonly IUserPreferenceService _service;
    private readonly ILogger<UserPreferencesController> _logger;

    public UserPreferencesController(
        IUserPreferenceService service, 
        ILogger<UserPreferencesController> logger)
    {
        _service = service;
        _logger = logger;
        Logger = logger;
        _logger.LogInformation("UserPreferencesController initialized");
    }

    // GET: UserPreferences/Edit - Edit current user's preferences
    public async Task<IActionResult> Edit()
    {
        _logger.LogInformation("GET Edit Preferences - User: {Username}", CurrentUser?.UserName);
        
        var response = await _service.GetByUserIdAsync(UserId!.Value);
        
        // If no preferences exist, create new model
        var model = response.Success && response.Data != null
            ? response.Data
            : new UserPreference { UserId = UserId!.Value };

        return View(model);
    }

    // POST: UserPreferences/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserPreference model)
    {
        _logger.LogInformation("POST Edit Preferences - User: {Username}", CurrentUser?.UserName);
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Ensure user can only edit their own preferences
        model.UserId = UserId!.Value;

        var response = await _service.CreateOrUpdateAsync(model, UserId);
        
        return HandleServiceResponse(response, "Edit");
    }
}
```

---

### 4. PersonalityTraits Module

Similar structure to UserPreferences - user edits their own personality trait.

---

## 🎨 View Patterns

### Standard CRUD View Structure

#### Index View Pattern
```cshtml
- Header với title và "Create New" button
- Search/Filter form
- Results (table hoặc grid)
- Actions (View, Edit, Delete buttons)
- Empty state khi không có data
```

#### Create/Edit View Pattern
```cshtml
- Header với title
- Form với validation
- Input fields (với Tailwind styling)
- Save và Cancel buttons
- Validation scripts
```

#### Details View Pattern
```cshtml
- Header với title
- Display fields (read-only)
- Edit và Delete buttons
- Back to List button
```

#### Delete View Pattern
```cshtml
- Header với warning
- Display key fields
- Confirmation form
- Delete và Cancel buttons
```

---

## 🚀 Quick Implementation Guide

### 1. Create Controller
1. Inject service và logger
2. Set Logger property từ BaseController
3. Add logging cho mọi action
4. Implement CRUD methods
5. Sử dụng HandleServiceResponse() từ BaseController
6. Check authorization (Admin hoặc Owner)

### 2. Create Views
1. Copy từ Users hoặc Roles views
2. Thay đổi model
3. Update fields
4. Adjust layout nếu cần
5. Test responsive design

### 3. Update Navigation
Add menu items trong `_Layout.cshtml`:
```cshtml
@if (User.Identity?.IsAuthenticated == true)
{
    <a asp-controller="Photos" asp-action="Index" class="...">Photos</a>
    <a asp-controller="Messages" asp-action="Index" class="...">
        Messages
        <span id="unreadBadge" class="...">0</span>
    </a>
    <a asp-controller="MatchResults" asp-action="Index" class="...">Matches</a>
}
```

---

## ✅ Status Summary

### Hoàn chỉnh (100%)
- ✅ Entity Models
- ✅ DbContext
- ✅ Repositories
- ✅ Services
- ✅ Migrations
- ✅ Registration (DI)

### Đã có Controllers (7/12)
- ✅ Users
- ✅ Roles
- ✅ Auth
- ✅ Home
- ✅ Admin
- ✅ Photos
- ✅ Messages

### Cần tạo Controllers (5/12)
- ⏳ MatchResults
- ⏳ UserPreferences
- ⏳ PersonalityTraits

### Views Completion
- ✅ Auth (2/2 views)
- ✅ Users (5/5 views)
- ✅ Roles (5/5 views)
- 🔄 Photos (1/5 views) - cần thêm Create, Edit, Details, Delete
- 🔄 Messages (2/2 views) - done
- ⏳ MatchResults (0/2 views needed)
- ⏳ UserPreferences (0/1 view needed)
- ⏳ PersonalityTraits (0/1 view needed)

---

**Estimated time to complete all remaining:**
- Controllers: 1-2 hours
- Views: 2-3 hours
- Total: 3-5 hours

Hoặc tôi có thể tiếp tục tạo tất cả nếu bạn muốn! 🚀

