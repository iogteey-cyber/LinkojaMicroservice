# Linkoja Mobile App - Flutter Implementation Guide

## Overview

This document provides a comprehensive guide for building the Linkoja mobile application using Flutter. The app connects to the LinkojaMicroservice backend API and implements all features defined in the FRD (Functional Requirements Document).

**Backend API**: The Flutter app will consume 36 REST API endpoints from the LinkojaMicroservice
**API Documentation**: Available at `https://api.linkoja.com/swagger`

## Quick Start

### Prerequisites

- Flutter SDK 3.10.0 or higher
- Dart 3.0.0 or higher
- Android Studio / VS Code with Flutter extensions
- iOS development: Xcode 14+ (for Mac only)

### Create Project

```bash
flutter create linkoja_mobile
cd linkoja_mobile
flutter pub add provider dio retrofit flutter_secure_storage google_sign_in geolocator cached_network_image flutter_rating_bar
flutter pub add --dev build_runner retrofit_generator json_serializable
```

### Backend API Endpoints (36 Total)

#### Authentication (6 endpoints)
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/social-login` - Google OAuth login
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password with token
- `POST /api/auth/change-password` - Change password (authenticated)

#### OTP Verification (3 endpoints)
- `POST /api/verification/send-otp` - Send OTP via SMS/Email
- `POST /api/verification/verify-otp` - Verify OTP code
- `POST /api/verification/resend-otp` - Resend OTP

#### Business Management (14 endpoints)
- `GET /api/business` - List businesses (with geolocation filters)
- `GET /api/business/{id}` - Get business details
- `POST /api/business` - Create business
- `PUT /api/business/{id}` - Update business
- `DELETE /api/business/{id}` - Delete business
- `GET /api/business/my-businesses` - Get user's businesses
- `GET /api/business/{id}/insights` - Get business analytics
- `POST /api/business/{id}/reviews` - Add review
- `POST /api/business/reviews/{reviewId}/report` - Report review
- `POST /api/business/{id}/follow` - Follow business
- `DELETE /api/business/{id}/follow` - Unfollow business
- `POST /api/business/{id}/posts` - Create business post

#### Notifications (4 endpoints)
- `GET /api/notification` - Get notifications
- `GET /api/notification/unread-count` - Get unread count
- `PUT /api/notification/{id}/read` - Mark as read
- `PUT /api/notification/read-all` - Mark all as read

#### Admin Dashboard (7 endpoints)
- `GET /api/admin/businesses/pending` - View pending approvals
- `POST /api/admin/businesses/{id}/approve` - Approve/reject business
- `GET /api/admin/analytics` - Platform analytics
- `GET /api/admin/reports/reviews` - View review reports
- `PUT /api/admin/reports/reviews/{reportId}/resolve` - Resolve report

## Project Structure

```
linkoja_mobile/
├── lib/
│   ├── main.dart
│   ├── app.dart
│   ├── config/
│   │   ├── app_config.dart          # API URLs, constants
│   │   ├── routes.dart               # Navigation routes
│   │   └── theme.dart                # App theme
│   ├── core/
│   │   ├── constants/
│   │   │   ├── api_constants.dart    # API endpoints
│   │   │   └── storage_keys.dart     # Secure storage keys
│   │   ├── utils/
│   │   │   ├── validators.dart       # Form validators
│   │   │   ├── phone_formatter.dart  # Nigerian phone format
│   │   │   └── date_formatter.dart   # Date utilities
│   │   └── widgets/
│   │       ├── custom_button.dart
│   │       ├── custom_textfield.dart
│   │       └── loading_indicator.dart
│   ├── data/
│   │   ├── models/
│   │   │   ├── user.dart             # User model
│   │   │   ├── business.dart         # Business model
│   │   │   ├── review.dart           # Review model
│   │   │   ├── notification.dart     # Notification model
│   │   │   └── post.dart             # Post model
│   │   ├── repositories/
│   │   │   ├── auth_repository.dart  # Auth API calls
│   │   │   ├── business_repository.dart
│   │   │   └── notification_repository.dart
│   │   └── services/
│   │       ├── api_service.dart      # Retrofit API definitions
│   │       ├── storage_service.dart  # Secure storage wrapper
│   │       └── location_service.dart # Geolocation service
│   ├── providers/
│   │   ├── auth_provider.dart        # Auth state management
│   │   ├── business_provider.dart    # Business state
│   │   └── notification_provider.dart
│   └── ui/
│       ├── screens/
│       │   ├── auth/
│       │   │   ├── login_screen.dart
│       │   │   ├── register_screen.dart
│       │   │   ├── otp_verification_screen.dart
│       │   │   └── forgot_password_screen.dart
│       │   ├── home/
│       │   │   ├── home_screen.dart
│       │   │   ├── search_screen.dart
│       │   │   └── map_view_screen.dart
│       │   ├── business/
│       │   │   ├── business_detail_screen.dart
│       │   │   ├── create_business_screen.dart
│       │   │   └── my_businesses_screen.dart
│       │   ├── profile/
│       │   │   ├── profile_screen.dart
│       │   │   └── edit_profile_screen.dart
│       │   ├── admin/
│       │   │   ├── admin_dashboard_screen.dart
│       │   │   └── business_approval_screen.dart
│       │   └── notifications/
│       │       └── notifications_screen.dart
│       └── widgets/
│           ├── business_card.dart
│           ├── review_card.dart
│           └── post_card.dart
├── test/
│   ├── unit/
│   ├── widget/
│   └── integration/
└── assets/
    ├── images/
    ├── icons/
    └── fonts/
```

## Key Dependencies

Add to `pubspec.yaml`:

```yaml
dependencies:
  # State Management
  provider: ^6.1.1
  
  # API & Networking
  dio: ^5.4.0
  retrofit: ^4.0.3
  json_annotation: ^4.8.1
  
  # Storage
  flutter_secure_storage: ^9.0.0
  shared_preferences: ^2.2.2
  
  # Authentication
  google_sign_in: ^6.1.5
  
  # Location
  geolocator: ^10.1.0
  geocoding: ^2.1.1
  google_maps_flutter: ^2.5.0
  
  # UI Components
  cached_network_image: ^3.3.0
  flutter_rating_bar: ^4.0.1
  pin_code_fields: ^8.0.1
  shimmer: ^3.0.0
  pull_to_refresh: ^2.0.0
  image_picker: ^1.0.4
  
  # Utilities
  intl: ^0.18.1
  url_launcher: ^6.2.1
  connectivity_plus: ^5.0.2

dev_dependencies:
  build_runner: ^2.4.6
  retrofit_generator: ^8.0.4
  json_serializable: ^6.7.1
  mockito: ^5.4.3
```

## Configuration Files

See the following detailed implementation guides:
- [API Integration Guide](./API_INTEGRATION.md) - Complete API setup
- [Authentication Flow](./AUTHENTICATION.md) - Login, register, OTP
- [Features Implementation](./FEATURES.md) - All app features
- [UI Components](./UI_COMPONENTS.md) - Reusable widgets

## Features Checklist

### Phase 1: Core Features ✅
- [x] User Registration & Login
- [x] Google OAuth Integration
- [x] OTP Phone Verification
- [x] Password Reset Flow
- [x] Business Listing with Geolocation
- [x] Business Detail View
- [x] Search & Filters
- [x] Reviews & Ratings
- [x] Follow/Unfollow Businesses
- [x] Notifications

### Phase 2: Business Management ✅
- [x] Create/Edit Business
- [x] Business Posts
- [x] Business Insights/Analytics
- [x] Photo Upload Support

### Phase 3: Admin Features ✅
- [x] Admin Dashboard
- [x] Business Approval/Rejection
- [x] Review Moderation
- [x] Platform Analytics

### Phase 4: Future Enhancements
- [ ] In-app Messaging (Phase 2 per FRD)
- [ ] Push Notifications
- [ ] File Upload to Cloud Storage
- [ ] Dark Mode
- [ ] Offline Mode

## Running the App

### Development
```bash
# Run on Android emulator
flutter run

# Run on iOS simulator (Mac only)
flutter run -d ios

# Run with specific API endpoint
flutter run --dart-define=API_URL=https://api.linkoja.com
```

### Build for Production

```bash
# Android APK
flutter build apk --release

# Android App Bundle
flutter build appbundle --release

# iOS
flutter build ios --release
```

## Environment Variables

Create `.env` file in project root:

```env
API_BASE_URL=https://api.linkoja.com
GOOGLE_CLIENT_ID=1027186401789-gmlrvq5qq51ffga0nhepf3173ldc20oq.apps.googleusercontent.com
```

## Testing

```bash
# Run all tests
flutter test

# Run specific test
flutter test test/unit/auth_provider_test.dart

# Run with coverage
flutter test --coverage
```

## API Integration Summary

The Flutter app communicates with the backend via REST APIs with JWT authentication:

1. **Authentication Flow**:
   - User logs in → Receive JWT token
   - Store token in FlutterSecureStorage
   - Add token to all API requests via Dio interceptor

2. **Geolocation**:
   - Get user's GPS coordinates
   - Send latitude/longitude to business search API
   - Filter by radius (default 5km)

3. **OTP Verification**:
   - Backend sends SMS via Termii gateway
   - User enters 6-digit code
   - Backend validates and marks phone as verified

4. **Google OAuth**:
   - Get Google ID token from GoogleSignIn
   - Send token to backend
   - Backend validates with Google API
   - Returns JWT token for session

## Next Steps

1. **Read the detailed guides** in this FlutterApp folder
2. **Set up the project** with required dependencies
3. **Configure API endpoints** to your deployed backend
4. **Start with authentication** screens
5. **Implement home & business features**
6. **Add admin functionality**
7. **Test on real devices**
8. **Prepare for app store release**

For detailed implementation of each feature, refer to:
- `API_INTEGRATION.md` - Complete API service setup
- `AUTHENTICATION.md` - Full auth flow with code
- `FEATURES.md` - All app features with examples
- `UI_COMPONENTS.md` - Reusable widgets and UI patterns

---

**Note**: This Flutter app is designed to work with the LinkojaMicroservice backend. Ensure the backend is deployed and accessible before running the mobile app.
