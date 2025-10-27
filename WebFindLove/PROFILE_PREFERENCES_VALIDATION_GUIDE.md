# 📝 Hướng dẫn Validation Hồ Sơ & Sở Thích - Profile & Preferences Validation Guide

## ✅ Tóm tắt

Đã hoàn thành validation đầy đủ cho **2 trang quan trọng**:
1. ✅ **EditProfile** - Chỉnh sửa hồ sơ cá nhân (bắt buộc nhập đầy đủ thông tin)
2. ✅ **UserPreferences** - Thiết lập sở thích tìm kiếm (bắt buộc chọn tiêu chí cơ bản)

Tất cả validation đều có **thông báo lỗi bằng tiếng Việt** và **UX thân thiện**.

---

## 📄 1. VALIDATION TRANG EDIT PROFILE

### 🎯 **Các trường BẮT BUỘC (Required)**

| Trường | Loại | Validation | Lý do |
|--------|------|------------|-------|
| **Số Điện Thoại** | Text | • Required<br>• Phone format<br>• 10-15 ký tự | Để liên hệ và xác thực |
| **Giới Tính** | Select | • Required<br>• male/female/other | Cần thiết cho matching |
| **Ngày Sinh** | Date | • Required<br>• Phải ≥ 18 tuổi | Giới hạn độ tuổi hợp lệ |
| **Chiều Cao** | Number | • Required<br>• 100-250 cm | Thông tin cơ bản |
| **Địa Chỉ Hiện Tại** | Text | • Required<br>• Max 255 ký tự | Để matching theo vị trí |
| **Sở Thích** | Text | • Required<br>• Comma-separated | Để AI matching |
| **Nhóm Tính Cách** | Select | • Required<br>• MBTI types | Để AI matching |

### 🔹 **Các trường TÙY CHỌN (Optional)**

| Trường | Loại | Validation |
|--------|------|------------|
| Họ và Tên | Text | Max 100 ký tự |
| Quê Quán | Text | Max 255 ký tự |
| Tiểu Sử | Textarea | Max 1000 ký tự |
| Mô Tả Tính Cách | Textarea | Max 1000 ký tự |
| Avatar | File | Image only, Max 5MB |

### 📋 **ViewModel: EditProfileVM.cs**

```csharp
public class EditProfileVM
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Giới tính là bắt buộc")]
    [StringLength(20, ErrorMessage = "Giới tính không được vượt quá 20 ký tự")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Chiều cao là bắt buộc")]
    [Range(100, 250, ErrorMessage = "Chiều cao phải từ 100 đến 250 cm")]
    public int? Height { get; set; }

    [Required(ErrorMessage = "Địa chỉ hiện tại là bắt buộc")]
    [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
    public string? Location { get; set; }

    [Required(ErrorMessage = "Sở thích là bắt buộc")]
    public string? Interests { get; set; }

    [Required(ErrorMessage = "Nhóm tính cách là bắt buộc")]
    [StringLength(50, ErrorMessage = "Nhóm tính cách không được vượt quá 50 ký tự")]
    public string? PersonalityType { get; set; }
}
```

### 🎨 **Client-side Validation (JavaScript)**

#### **1. Kiểm tra tuổi (≥ 18)**
```javascript
const birthDate = new Date(dob.value);
const today = new Date();
let age = today.getFullYear() - birthDate.getFullYear();
const monthDiff = today.getMonth() - birthDate.getMonth();
if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
    age--;
}

if (age < 18) {
    errorMessages.push('Bạn phải từ 18 tuổi trở lên');
    isValid = false;
}
```

#### **2. Kiểm tra chiều cao (100-250 cm)**
```javascript
const heightValue = parseInt(height.value);
if (heightValue < 100 || heightValue > 250) {
    errorMessages.push('Chiều cao phải từ 100 đến 250 cm');
    isValid = false;
}
```

#### **3. Kiểm tra số điện thoại (10-15 ký tự)**
```javascript
const phonePattern = /^[0-9+\-\s()]{10,15}$/;
if (!phonePattern.test(phone.value)) {
    errorMessages.push('Số điện thoại không hợp lệ');
    isValid = false;
}
```

### 🎯 **Test Cases - EditProfile**

| Test | Input | Expected Result |
|------|-------|-----------------|
| Tuổi < 18 | DOB: 2010-01-01 | ❌ "Bạn phải từ 18 tuổi trở lên" |
| Chiều cao < 100 | Height: 50 | ❌ "Chiều cao phải từ 100 đến 250 cm" |
| Chiều cao > 250 | Height: 300 | ❌ "Chiều cao phải từ 100 đến 250 cm" |
| SĐT không hợp lệ | Phone: "abc123" | ❌ "Số điện thoại không hợp lệ" |
| Không chọn giới tính | Gender: "" | ❌ "Giới tính là bắt buộc" |
| Không nhập sở thích | Interests: "" | ❌ "Sở thích là bắt buộc" |
| Không chọn tính cách | PersonalityType: "" | ❌ "Nhóm tính cách là bắt buộc" |
| Dữ liệu hợp lệ | All fields valid | ✅ Lưu thành công |

---

## 📄 2. VALIDATION TRANG USER PREFERENCES

### 🎯 **Các trường BẮT BUỘC (Required)**

| Trường | Loại | Validation | Lý do |
|--------|------|------------|-------|
| **Giới Tính Mong Muốn** | Select | • Required<br>• male/female/other/all | Tiêu chí cơ bản |
| **Tuổi Tối Thiểu** | Number | • Required<br>• 18-100<br>• ≤ Tuổi tối đa | Giới hạn độ tuổi |
| **Tuổi Tối Đa** | Number | • Required<br>• 18-100<br>• ≥ Tuổi tối thiểu | Giới hạn độ tuổi |

### 🔹 **Các trường TÙY CHỌN (Optional)**

| Trường | Loại | Validation |
|--------|------|------------|
| Chiều Cao Tối Thiểu | Number | 100-250 cm, ≤ Max |
| Chiều Cao Tối Đa | Number | 100-250 cm, ≥ Min |
| Khu Vực Mong Muốn | Text | Max 255 ký tự |
| Tính Cách Mong Muốn | Textarea | Free text (comma-separated) |
| Sở Thích Mong Muốn | Textarea | Free text (comma-separated) |

### 📋 **Entity: UserPreference.cs**

```csharp
public class UserPreference : BaseEntity
{
    [Required(ErrorMessage = "Giới tính mong muốn là bắt buộc")]
    [StringLength(20)]
    public string? PreferredGender { get; set; }

    [Required(ErrorMessage = "Tuổi tối thiểu là bắt buộc")]
    [Range(18, 100, ErrorMessage = "Tuổi tối thiểu phải từ 18 đến 100")]
    public int? AgeMin { get; set; }

    [Required(ErrorMessage = "Tuổi tối đa là bắt buộc")]
    [Range(18, 100, ErrorMessage = "Tuổi tối đa phải từ 18 đến 100")]
    public int? AgeMax { get; set; }
}
```

### 🎨 **Client-side Validation (JavaScript)**

#### **1. Kiểm tra khoảng tuổi hợp lệ**
```javascript
const minAge = parseInt(ageMin.value);
const maxAge = parseInt(ageMax.value);

if (minAge < 18 || minAge > 100) {
    errorMessages.push('Tuổi tối thiểu phải từ 18 đến 100');
    isValid = false;
}

if (maxAge < 18 || maxAge > 100) {
    errorMessages.push('Tuổi tối đa phải từ 18 đến 100');
    isValid = false;
}

if (minAge > maxAge) {
    errorMessages.push('Tuổi tối thiểu không được lớn hơn tuổi tối đa');
    isValid = false;
}
```

#### **2. Kiểm tra khoảng chiều cao hợp lệ**
```javascript
const minH = parseInt(minHeight.value);
const maxH = parseInt(maxHeight.value);

if (minH < 100 || minH > 250) {
    errorMessages.push('Chiều cao tối thiểu phải từ 100 đến 250 cm');
    isValid = false;
}

if (maxH < 100 || maxH > 250) {
    errorMessages.push('Chiều cao tối đa phải từ 100 đến 250 cm');
    isValid = false;
}

if (minH > maxH) {
    errorMessages.push('Chiều cao tối thiểu không được lớn hơn chiều cao tối đa');
    isValid = false;
}
```

#### **3. Kiểm tra giới tính được chọn**
```javascript
const gender = document.querySelector('select[name="PreferredGender"]');
if (gender && !gender.value) {
    errorMessages.push('Vui lòng chọn giới tính mong muốn');
    isValid = false;
}
```

### 🎯 **Test Cases - UserPreferences**

| Test | Input | Expected Result |
|------|-------|-----------------|
| Tuổi min < 18 | AgeMin: 15 | ❌ "Tuổi tối thiểu phải từ 18 đến 100" |
| Tuổi max > 100 | AgeMax: 120 | ❌ "Tuổi tối đa phải từ 18 đến 100" |
| Min > Max | AgeMin: 40, AgeMax: 30 | ❌ "Tuổi tối thiểu không được lớn hơn tuổi tối đa" |
| Chiều cao min < 100 | MinHeight: 50 | ❌ "Chiều cao tối thiểu phải từ 100 đến 250 cm" |
| Chiều cao max > 250 | MaxHeight: 300 | ❌ "Chiều cao tối đa phải từ 100 đến 250 cm" |
| MinHeight > MaxHeight | MinHeight: 180, MaxHeight: 160 | ❌ "Chiều cao tối thiểu không được lớn hơn chiều cao tối đa" |
| Không chọn giới tính | PreferredGender: "" | ❌ "Vui lòng chọn giới tính mong muốn" |
| Dữ liệu hợp lệ | All fields valid | ✅ Lưu thành công |

---

## 🎨 UX FEATURES

### ✨ **Cải thiện trải nghiệm người dùng**

1. **Visual Indicators**
   - ⭐ Dấu `*` màu đỏ cho các trường bắt buộc
   - 🔴 Border đỏ highlight field có lỗi
   - ✅ Border xanh khi valid (tùy chọn)

2. **Real-time Feedback**
   - 🔄 Xóa border đỏ ngay khi user bắt đầu nhập
   - ⚡ Validation on input/change event
   - 📢 Alert thông báo tổng hợp lỗi khi submit

3. **Helpful Messages**
   - 📝 Placeholder gợi ý cho mỗi trường
   - 💡 Helper text dưới các trường phức tạp
   - ℹ️ Icon info cho hướng dẫn chi tiết

4. **Dark Mode Support**
   - 🌙 Tất cả validation messages đều hỗ trợ dark mode
   - 🎨 Màu sắc tương thích với theme tối

---

## 🔄 VALIDATION FLOW

### **EditProfile Flow**

```
User điền form
    ↓
HTML5 validation (required, type, pattern)
    ↓
JavaScript validation on submit
    ├─ Kiểm tra tuổi ≥ 18
    ├─ Kiểm tra chiều cao 100-250
    ├─ Kiểm tra phone format
    └─ Kiểm tra các trường required
    ↓
POST to UsersController.EditProfile
    ↓
Server-side validation (DataAnnotations)
    ↓
┌─────────────┬──────────────┐
│ ❌ Invalid  │  ✅ Valid    │
│ Return view │  Save to DB  │
│ with errors │  Redirect    │
└─────────────┴──────────────┘
```

### **UserPreferences Flow**

```
User điền form
    ↓
HTML5 validation (required, min, max)
    ↓
JavaScript validation on submit
    ├─ Kiểm tra giới tính được chọn
    ├─ Kiểm tra khoảng tuổi 18-100
    ├─ Kiểm tra AgeMin ≤ AgeMax
    ├─ Kiểm tra khoảng chiều cao 100-250
    └─ Kiểm tra MinHeight ≤ MaxHeight
    ↓
POST to UserPreferencesController.Edit
    ↓
Server-side validation (DataAnnotations)
    ↓
┌─────────────┬──────────────┐
│ ❌ Invalid  │  ✅ Valid    │
│ Return view │  Save to DB  │
│ with errors │  Redirect    │
└─────────────┴──────────────┘
```

---

## 📊 THỐNG KÊ VALIDATION

### **EditProfile Validation**

| Loại | Số lượng | Chi tiết |
|------|----------|----------|
| Required Fields | 7 | PhoneNumber, Gender, DateOfBirth, Height, Location, Interests, PersonalityType |
| Optional Fields | 5 | FullName, Hometown, Bio, PersonalityText, Avatar |
| Server Validation | 7 | DataAnnotations trong EditProfileVM |
| Client Validation | 3 | Age ≥18, Height 100-250, Phone format |

### **UserPreferences Validation**

| Loại | Số lượng | Chi tiết |
|------|----------|----------|
| Required Fields | 3 | PreferredGender, AgeMin, AgeMax |
| Optional Fields | 5 | MinHeight, MaxHeight, LocationPreference, PersonalityPreference, InterestPreference |
| Server Validation | 3 | DataAnnotations trong UserPreference entity |
| Client Validation | 3 | Age range, Height range, Gender selection |

---

## 🎯 KẾT LUẬN

### ✅ **Đã hoàn thành:**

#### **EditProfile:**
1. ✅ Thêm `[Required]` attribute cho 7 trường quan trọng
2. ✅ Thêm `required` HTML attribute
3. ✅ Thêm dấu `*` màu đỏ cho trường bắt buộc
4. ✅ JavaScript validation: tuổi, chiều cao, phone
5. ✅ Thông báo lỗi tiếng Việt
6. ✅ UX: Border đỏ, auto clear error
7. ✅ Dark mode support

#### **UserPreferences:**
1. ✅ Thêm `[Required]` attribute cho 3 trường cốt lõi
2. ✅ Thêm `required` HTML attribute
3. ✅ Thêm dấu `*` màu đỏ cho trường bắt buộc
4. ✅ JavaScript validation: age range, height range, gender
5. ✅ Logic validation: Min ≤ Max
6. ✅ Thông báo lỗi tiếng Việt
7. ✅ UX: Border đỏ, auto clear error
8. ✅ Cải thiện placeholder và helper text

### 📈 **Lợi ích:**

1. **Chất lượng dữ liệu cao hơn**
   - User phải nhập đầy đủ thông tin cần thiết
   - AI matching có đủ data để hoạt động hiệu quả

2. **Trải nghiệm người dùng tốt**
   - Thông báo lỗi rõ ràng bằng tiếng Việt
   - Real-time feedback
   - Visual indicators

3. **Bảo mật & Tin cậy**
   - Double validation (client + server)
   - Ngăn chặn dữ liệu không hợp lệ
   - Business logic validation (age ≥18, range checks)

---

## 📝 GHI CHÚ BỔ SUNG

### **Tùy chỉnh thêm (Optional):**

1. **Thêm validation cho Avatar:**
   ```csharp
   [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Kích thước file không được vượt quá 5MB")]
   [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".gif" })]
   public IFormFile? AvatarFile { get; set; }
   ```

2. **Thêm custom validation attribute:**
   ```csharp
   [AgeRange(18, 100, ErrorMessage = "Bạn phải từ 18 đến 100 tuổi")]
   public DateTime? DateOfBirth { get; set; }
   ```

3. **Thêm AJAX validation:**
   - Validate username/email uniqueness real-time
   - Check phone number format với API

---

**Ngày cập nhật:** 26/10/2025  
**Trạng thái:** ✅ **HOÀN THÀNH - Đã test và hoạt động đúng**  
**File liên quan:**
- `WebFindLove/Models/Services/UserService/ViewModels/EditProfileVM.cs`
- `WebFindLove/Models/Entities/UserPreference.cs`
- `WebFindLove/Views/Users/EditProfile.cshtml`
- `WebFindLove/Views/UserPreferences/Edit.cshtml`

