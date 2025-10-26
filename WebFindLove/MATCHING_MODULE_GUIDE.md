# Module Ghép Đôi (Matching) - Hướng Dẫn Hoàn Chỉnh

## 📋 Tổng Quan

Module Matching cung cấp tính năng ghép đôi thông minh sử dụng AI để tìm những người phù hợp dựa trên:
- **Thông tin cá nhân** (profile embedding)
- **Sở thích tìm kiếm** (preference embedding)
- **Độ tương thích hai chiều** (mutual matching)

## 🏗️ Kiến Trúc

### Controller
**File**: `WebFindLove/Controllers/MatchingController.cs`

#### Actions:
1. **Index** (GET)
   - Trang chủ module Matching
   - Hiển thị 2 lựa chọn: Tìm người phù hợp & Người phù hợp hai chiều
   - Hiển thị số lượng matches hiện có
   - Route: `/Matching/Index`

2. **FindMatches** (GET)
   - Tìm và hiển thị danh sách người phù hợp với user hiện tại
   - Tính toán điểm tương thích bằng AI
   - Lưu kết quả vào database
   - Route: `/Matching/FindMatches`

3. **MutualMatches** (GET)
   - Tìm những người phù hợp hai chiều (mutual matches)
   - Logic tối ưu: giảm số lần query database
   - Chỉ hiển thị những người cũng chọn user hiện tại
   - Route: `/Matching/MutualMatches`

4. **DeleteMatch** (POST)
   - Xóa một match khỏi danh sách (soft delete)
   - Yêu cầu antiforgery token
   - Route: `/Matching/DeleteMatch`

5. **RefreshMatches** (POST) - API
   - Cập nhật danh sách matches mới nhất
   - Trả về JSON với số lượng matches tìm thấy
   - Route: `/Matching/RefreshMatches`

6. **GetMatchCount** (GET) - API
   - Lấy số lượng matches hiện có
   - Trả về JSON với count và topScore
   - Route: `/Matching/GetMatchCount`

### Services

#### MatchingService
**File**: `WebFindLove/Models/Services/MatchingService/MatchingService.cs`

**Chức năng chính**:
- `FindBestMatchesAsync(Guid userId)`: Tìm matches tốt nhất cho user
- `GetCandidateUsersAsync(...)`: Lọc ứng viên theo tiêu chí
- `ComputeCosineSimilarity(...)`: Tính độ tương đồng giữa 2 vectors
- `ParseEmbedding(...)`: Parse JSON embedding thành float array

**Thuật toán Matching**:
```
1. Lấy thông tin user A và preference của A
2. Kiểm tra embeddings của A (profile + preference)
3. Tìm danh sách ứng viên phù hợp (filter theo gender, age, height)
4. Với mỗi ứng viên B:
   - Lấy embeddings của B
   - Tính sim1 = cosine_similarity(preferenceA, profileB)  // A muốn gì vs B là gì
   - Tính sim2 = cosine_similarity(preferenceB, profileA)  // B muốn gì vs A là gì
   - matchScore = ((sim1 + sim2) / 2 + 1) / 2 * 100  // Chuyển từ [-1,1] sang [0,100]
5. Lưu kết quả vào database, sắp xếp theo điểm
```

#### MatchResultService
**File**: `WebFindLove/Models/Services/MatchResultService/MatchResultService.cs`

**Chức năng**:
- `GetMatchesByUserIdAsync(Guid userId)`: Lấy tất cả matches của user
- `GetTopMatchesAsync(Guid userId, int count)`: Lấy top N matches
- `GetMatchBetweenUsersAsync(Guid userId1, Guid userId2)`: Kiểm tra match giữa 2 users
- `CreateMatchAsync(...)`: Tạo match mới
- `DeleteMatchAsync(Guid id)`: Xóa match (soft delete)

### Repositories

#### MatchResultRepository
**File**: `WebFindLove/Models/Repositories/MatchResultRepo/MatchResultRepository.cs`

**Đặc điểm**:
- Tự động Include navigation properties (User, MatchedUser)
- Filter chỉ lấy matches IsActive = true
- Sắp xếp theo MatchScore giảm dần

### Views

#### Index.cshtml
**File**: `WebFindLove/Views/Matching/Index.cshtml`

**Tính năng**:
- Hiển thị 2 cards lựa chọn với gradient đẹp
- Stats card hiển thị số lượng matches hiện có
- Nút "Cập Nhật Ngay" để refresh matches
- AJAX call để load match count real-time
- Responsive design với Tailwind CSS

#### FindMatches.cshtml
**File**: `WebFindLove/Views/Matching/FindMatches.cshtml`

**Tính năng**:
- Hiển thị danh sách người phù hợp
- Stats cards: Tổng số, Điểm trung bình, Điểm cao nhất
- Mỗi match hiển thị:
  - Avatar/placeholder
  - Thông tin cơ bản (tuổi, chiều cao, địa điểm)
  - Bio và interests
  - Điểm tương thích với màu sắc phân loại
  - Phân tích AI
  - Nút: Nhắn tin, Xem hồ sơ, Xóa
- Empty state khi chưa có matches

#### MutualMatches.cshtml
**File**: `WebFindLove/Views/Matching/MutualMatches.cshtml`

**Tính năng**:
- Thiết kế đặc biệt cho mutual matches (màu purple)
- Badge "Mutual" với icon handshake
- Top 3 matches có badge đặc biệt (🥇🥈🥉)
- Info banner giải thích mutual matching
- Hiển thị thêm occupation và personality type
- Nút "Nhắn Tin Ngay" với gradient đẹp hơn
- Empty state khuyến khích chủ động nhắn tin

## 🎨 UI/UX Features

### Color Scheme
- **One-way matches**: Pink to Red gradient
- **Mutual matches**: Purple to Indigo gradient
- **Stats**: Blue gradient
- **Score colors**:
  - ≥80: Green (Tuyệt vời)
  - ≥60: Blue (Tốt)
  - <60: Gray (Khá)

### Responsive Design
- Desktop: 2 columns grid
- Mobile: 1 column stack
- Touch-friendly buttons
- Smooth transitions & hover effects

### Interactive Elements
- Real-time match count loading
- AJAX refresh without page reload
- Spinning icon during loading
- Auto-hiding notifications (5 seconds)
- Smooth animations

## 🔧 Cách Sử Dụng

### Cho Người Dùng

1. **Chuẩn bị**:
   - Hoàn thiện hồ sơ cá nhân (Profile)
   - Thiết lập sở thích tìm kiếm (UserPreferences)
   - Hệ thống sẽ tự động tạo embeddings

2. **Tìm người phù hợp**:
   - Vào `/Matching/Index`
   - Click "Bắt Đầu Tìm Kiếm" hoặc "Cập Nhật Ngay"
   - Xem danh sách người phù hợp
   - Click "Nhắn Tin" để bắt đầu trò chuyện

3. **Xem mutual matches**:
   - Click "Xem Kết Nối Hai Chiều"
   - Ưu tiên nhắn tin với những người mutual
   - Cơ hội kết nối cao hơn

### Cho Developer

#### Thêm Service Mới
```csharp
// ServiceRegistration.cs
services.AddScoped<IYourService, YourService>();
```

#### Customize Matching Algorithm
```csharp
// MatchingService.cs - FindBestMatchesAsync
// Thay đổi công thức tính matchScore:
var matchScore = ((sim1 + sim2) / 2.0 + 1.0) / 2.0 * 100.0;
```

#### Thay đổi Criteria Lọc
```csharp
// MatchingService.cs - GetCandidateUsersAsync
// Thêm filter mới:
query = query.Where(u => u.YourField == yourValue);
```

## 📊 Database Schema

### MatchResult Table
```sql
CREATE TABLE MatchResults (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,      -- User A
    MatchedUserId UNIQUEIDENTIFIER NOT NULL, -- User B (người được match)
    MatchScore FLOAT NULL,                  -- 0-100
    AiReasoning NVARCHAR(2000) NULL,       -- Lý do ghép đôi
    IsActive BIT NOT NULL DEFAULT 1,       -- Soft delete
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (MatchedUserId) REFERENCES Users(Id)
);
```

### User Table (relevant fields)
```sql
CREATE TABLE Users (
    ...
    ProfileEmbedding NVARCHAR(MAX) NULL,   -- JSON array của float[]
    ...
);
```

### UserPreference Table (relevant fields)
```sql
CREATE TABLE UserPreferences (
    ...
    PreferenceEmbedding NVARCHAR(MAX) NULL, -- JSON array của float[]
    PreferredGender NVARCHAR(50) NULL,
    AgeMin INT NULL,
    AgeMax INT NULL,
    MinHeight INT NULL,
    MaxHeight INT NULL,
    ...
);
```

## 🔐 Security

- **Authorization**: Tất cả actions yêu cầu `[Authorize]`
- **Antiforgery Token**: POST requests có CSRF protection
- **Data Access**: Users chỉ xem được matches của họ
- **Soft Delete**: Matches không bị xóa vĩnh viễn

## 🚀 Performance Optimization

### Implemented
- ✅ Navigation properties eager loading (Include)
- ✅ MutualMatches: Dictionary lookup thay vì N queries
- ✅ Index trên UserId, MatchedUserId, IsActive
- ✅ Only load active matches
- ✅ Pagination ready (Take/Skip)

### Recommendations
- 🔄 Cache match results (Redis/Memory Cache)
- 🔄 Background job để tính matches định kỳ
- 🔄 Elasticsearch cho full-text search
- 🔄 CDN cho avatars

## 🧪 Testing

### Manual Testing
1. Tạo 2+ users với profiles đầy đủ
2. Set preferences khác nhau
3. Chạy FindMatches cho user A
4. Chạy FindMatches cho user B
5. Kiểm tra MutualMatches cho cả 2 users

### Expected Results
- User A thấy danh sách matches dựa trên preferences
- Điểm tương thích từ 0-100
- Mutual matches chỉ hiển thị khi cả 2 match nhau
- Refresh cập nhật danh sách mới

## 📝 Logging

Logger được cấu hình cho tất cả actions:
- Info: User actions, query results
- Debug: Match details, similarity scores
- Warning: Failed operations, no matches found
- Error: Exceptions với stack trace

## 🔮 Future Enhancements

- [ ] Machine Learning để cải thiện algorithm
- [ ] Swipe interface (Tinder-style)
- [ ] Match notifications
- [ ] Match history/analytics
- [ ] Feedback từ users để train model
- [ ] Video chat integration
- [ ] Icebreaker suggestions

## 📞 Troubleshooting

### Không tìm thấy matches
- ✅ Kiểm tra user đã có ProfileEmbedding chưa
- ✅ Kiểm tra UserPreference đã được set chưa
- ✅ Kiểm tra có users khác active không
- ✅ Xem logs để biết lý do filter

### Điểm tương thích thấp
- Embeddings có thể chưa chính xác
- Review lại profile và preferences
- Cần train lại model embedding

### Mutual matches không hiển thị
- Kiểm tra logic trong MutualMatches action
- Verify cả 2 users đều có matches với nhau
- Check database: cả 2 chiều phải có record

## 📚 Related Documentation

- [Clean Architecture Documentation](Clean_Architecture_Documentation.md)
- [Embedding Service Documentation](FILE_UPLOAD_SERVICE_DOCUMENTATION.md)
- [Controller & View Templates](CONTROLLER_VIEW_TEMPLATES.md)
- [Real-time Messaging](REAL_TIME_MESSAGING_SUMMARY.md)

---

**Version**: 1.0  
**Last Updated**: October 26, 2025  
**Author**: AI Assistant  
**Status**: ✅ Production Ready

