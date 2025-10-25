# WebFindLove - Views Documentation

## Overview
This document provides an overview of all the views created for the WebFindLove application with Tailwind CSS styling.

## Design Theme
- **Primary Color**: Pink (#ec4899)
- **Secondary Color**: Purple (#8b5cf6)
- **Design Style**: Modern, gradient-based with smooth animations
- **Icons**: Font Awesome 6.4.0
- **CSS Framework**: Tailwind CSS (via CDN)

## Views Structure

### 1. Layout & Shared Components

#### _Layout.cshtml
- **Location**: `Views/Shared/_Layout.cshtml`
- **Features**:
  - Responsive navigation with mobile menu
  - User authentication status display
  - Role-based menu items (Admin access to Users and Roles)
  - Dropdown user menu for authenticated users
  - Success/Error message alerts (auto-hide after 5 seconds)
  - Modern gradient design with Font Awesome icons

### 2. Authentication Views

#### Login (Auth/Login.cshtml)
- **Features**:
  - Username or Email input
  - Password with show/hide toggle
  - Remember me checkbox
  - Forgot password link
  - Error message display
  - Link to registration page
- **Design**: Centered card layout with gradient background

#### Register (Auth/Register.cshtml)
- **Features**:
  - Username, Email, Full Name, and Password fields
  - Password confirmation validation
  - Client-side password matching
  - Terms and conditions checkbox
  - Link to login page
- **Design**: Centered card layout with validation messages

### 3. Users Management Views

#### Users Index (Users/Index.cshtml)
- **Features**:
  - Search by keyword (username, email, or name)
  - Filter by role (Admin, NhanVien, User)
  - Filter by status (Active/Inactive)
  - Table view with user information
  - Avatar initials with gradient background
  - Role and status badges
  - Action buttons (View, Edit, Delete)
- **Design**: Card-based layout with data table

#### Users Create (Users/Create.cshtml)
- **Features**:
  - Form for creating new users
  - Fields: Username, Email, Full Name, Role, Password, IsActive
  - Role dropdown (populated from database)
  - Password show/hide toggle
  - Validation messages
- **Design**: Form layout with grid system

#### Users Edit (Users/Edit.cshtml)
- **Features**:
  - Edit user information
  - Fields: Username, Email, Full Name, Role, IsActive
  - Note about password change policy
  - Validation messages
- **Design**: Form layout with informational alerts

#### Users Details (Users/Details.cshtml)
- **Features**:
  - Complete user information display
  - User avatar with initials
  - Status and role badges
  - Created and updated timestamps
  - Action buttons (Edit, Delete, Back to List)
- **Design**: Header with gradient background and detailed information grid

#### Users Delete (Users/Delete.cshtml)
- **Features**:
  - Confirmation page before deletion
  - Display user information
  - Warning about irreversible action
  - Cancel and confirm buttons
- **Design**: Centered card with warning indicators

### 4. Roles Management Views

#### Roles Index (Roles/Index.cshtml)
- **Features**:
  - Search by keyword (name or description)
  - Filter by status (Active/Inactive)
  - Grid view with role cards
  - Each card shows: name, description, status, dates
  - Action buttons (View, Edit, Delete)
- **Design**: Card grid layout with gradient headers

#### Roles Create (Roles/Create.cshtml)
- **Features**:
  - Form for creating new roles
  - Fields: Name, Description, IsActive
  - Character limit indicators
  - Informational notes
  - Validation messages
- **Design**: Form layout with helper text

#### Roles Edit (Roles/Edit.cshtml)
- **Features**:
  - Edit role information
  - Fields: Name, Description, IsActive
  - Warning about name changes affecting users
  - Validation messages
- **Design**: Form layout with warning alerts

#### Roles Details (Roles/Details.cshtml)
- **Features**:
  - Complete role information display
  - User count for this role
  - List of users with this role (up to 6)
  - Created and updated timestamps
  - Action buttons (Edit, Delete, Back to List)
- **Design**: Header with gradient background and detailed information grid

#### Roles Delete (Roles/Delete.cshtml)
- **Features**:
  - Confirmation page before deletion
  - Display role information
  - Show count of users with this role
  - Warning about users affected
  - Cancel and confirm buttons
- **Design**: Centered card with multiple warning levels

### 5. Home Page

#### Home Index (Home/Index.cshtml)
- **Features**:
  - Hero section with welcome message
  - Different content for authenticated/unauthenticated users
  - Features section (Secure, Community, Matches)
  - Admin dashboard quick links (for admin users)
  - Call-to-action for non-authenticated users
- **Design**: Multi-section layout with gradients and animations

## Common UI Elements

### Color Scheme
- **Primary**: Pink gradient (#ec4899)
- **Secondary**: Purple (#8b5cf6)
- **Success**: Green
- **Warning**: Yellow
- **Error**: Red
- **Info**: Blue

### Button Styles
- **Primary Button**: Gradient from primary to secondary
- **Secondary Button**: Border with transparent background
- **Danger Button**: Red solid background
- **Default Button**: Gray border

### Form Elements
- All inputs have rounded corners
- Focus state with ring effect
- Validation messages in red
- Helper text in gray

### Cards
- White background with shadow
- Rounded corners
- Hover effects with shadow increase
- Some with gradient headers

### Badges
- Role badges: Color-coded (Admin=Red, NhanVien=Blue, User=Green)
- Status badges: Active=Green, Inactive=Red
- Rounded full shape with icons

### Icons
- Font Awesome 6.4.0
- Consistent icon usage across views
- Icons in buttons, headers, and badges

## Responsive Design
- Mobile-first approach
- Hamburger menu for mobile navigation
- Grid layouts adjust for different screen sizes
- Tables have horizontal scroll on mobile

## JavaScript Features
- Mobile menu toggle
- Password show/hide functionality
- Auto-hide alerts after 5 seconds
- Form validation (client-side)
- Password confirmation matching

## Notes for Developers
1. All forms use anti-forgery tokens
2. Model validation is implemented on all forms
3. TempData messages are displayed in the layout
4. Role-based access control in navigation
5. Consistent design patterns across all views
6. Font Awesome CDN for icons
7. Tailwind CSS CDN for styling

## URLs Structure

### Authentication
- `/Auth/Login` - Login page
- `/Auth/Register` - Registration page
- `/Auth/Logout` - Logout action

### Users Management
- `/Users` - List all users
- `/Users/Create` - Create new user
- `/Users/Edit/{id}` - Edit user
- `/Users/Details/{id}` - View user details
- `/Users/Delete/{id}` - Delete user

### Roles Management
- `/Roles` - List all roles
- `/Roles/Create` - Create new role
- `/Roles/Edit/{id}` - Edit role
- `/Roles/Details/{id}` - View role details
- `/Roles/Delete/{id}` - Delete role

### Home
- `/` or `/Home` - Home page
- `/Home/Privacy` - Privacy page

## Next Steps
1. Test all views with actual data
2. Ensure all controller actions are properly connected
3. Test responsive design on different devices
4. Review and adjust colors/spacing as needed
5. Add any additional features or pages as required
6. Consider adding pagination for large data sets
7. Implement advanced search features if needed
8. Add user profile pictures support
9. Implement password reset functionality
10. Add email verification for new registrations

