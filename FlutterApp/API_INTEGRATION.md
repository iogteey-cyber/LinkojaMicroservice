# Linkoja Mobile App - API Integration Guide

This guide explains how to integrate the Flutter app with the LinkojaMicroservice backend. The backend now wraps every response in a standard envelope. Update your client to parse this envelope and to use the latest request/response models and endpoints.

---

## Unified Response Envelope

All API responses (success and error) are wrapped as:

```json
{
 "isSuccessful": true,
 "response": {
 "code": "00",
 "description": "Operation successful",
 "data": { /* payload or null */ }
 }
}
```

- `isSuccessful`: overall success flag
- `response.code`: app-level code (e.g., "00" success)
- `response.description`: human-readable message
- `response.data`: typed payload (can be null)

You should parse this envelope and use `response.data` as the payload for your features.

---

## API Service Setup

###1) Dio Client

Add a basic `Dio` setup. No unwrapping in the interceptor; we will parse the envelope in typed models.

```dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class DioClient {
 final Dio _dio;
 final FlutterSecureStorage _storage;

 DioClient(this._storage)
 : _dio = Dio(
 BaseOptions(
 baseUrl: 'https://api.linkoja.com',
 connectTimeout: const Duration(seconds:30),
 receiveTimeout: const Duration(seconds:30),
 headers: const {
 'Content-Type': 'application/json',
 'Accept': 'application/json',
 },
 ),
 ) {
 _dio.interceptors.add(
 InterceptorsWrapper(
 onRequest: (options, handler) async {
 final token = await _storage.read(key: 'auth_token');
 if (token != null) {
 options.headers['Authorization'] = 'Bearer $token';
 }
 return handler.next(options);
 },
 onError: (DioException error, handler) async {
 if (error.response?.statusCode ==401) {
 await _storage.delete(key: 'auth_token');
 // Navigate to login screen
 }
 return handler.next(error);
 },
 ),
 );
 }

 Dio get dio => _dio;
}
```

###2) Envelope Models (Generic)

Use generic, `json_serializable`-friendly models for the envelope.

```dart
import 'package:json_annotation/json_annotation.dart';

part 'api_envelope.g.dart';

@JsonSerializable(genericArgumentFactories: true)
class ApiResponse<T> {
 final bool isSuccessful;
 final ResponseStatus<T> response;

 ApiResponse({required this.isSuccessful, required this.response});

 factory ApiResponse.fromJson(
 Map<String, dynamic> json,
 T Function(Object? json) fromJsonT,
 ) => _$ApiResponseFromJson(json, fromJsonT);
}

@JsonSerializable(genericArgumentFactories: true)
class ResponseStatus<T> {
 final String code;
 final String description;
 final T? data;

 ResponseStatus({
 required this.code,
 required this.description,
 this.data,
 });

 factory ResponseStatus.fromJson(
 Map<String, dynamic> json,
 T Function(Object? json) fromJsonT,
 ) => _$ResponseStatusFromJson(json, fromJsonT);
}

class Empty {
 const Empty();
}
```

Generate code:

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

---

## Data Models (aligned with backend)

### Auth Models

```dart
import 'package:json_annotation/json_annotation.dart';

part 'auth_models.g.dart';

@JsonSerializable()
class RegisterRequest {
 final String email;
 final String password;
 final String phone; // matches backend `Phone`
 final String name; // matches backend `Name`
 final String? socialId; // optional

 RegisterRequest({
 required this.email,
 required this.password,
 required this.phone,
 required this.name,
 this.socialId,
 });

 Map<String, dynamic> toJson() => _$RegisterRequestToJson(this);
}

@JsonSerializable()
class LoginRequest {
 final String email;
 final String password;
 LoginRequest({required this.email, required this.password});
 Map<String, dynamic> toJson() => _$LoginRequestToJson(this);
}

@JsonSerializable()
class SocialLoginRequest {
 final String provider; // matches backend `Provider`
 final String accessToken; // matches backend `AccessToken`
 final String? email;
 final String? name;
 final String? photoUrl;

 SocialLoginRequest({
 required this.provider,
 required this.accessToken,
 this.email,
 this.name,
 this.photoUrl,
 });

 Map<String, dynamic> toJson() => _$SocialLoginRequestToJson(this);
}

@JsonSerializable()
class AuthResponse {
 final String token;
 final UserDto user;

 AuthResponse({required this.token, required this.user});
 factory AuthResponse.fromJson(Map<String, dynamic> json) => _$AuthResponseFromJson(json);
}

@JsonSerializable()
class UserDto {
 final int id;
 final String email;
 final String? phone; // matches backend `Phone`
 final String name; // matches backend `Name`
 final String role;

 UserDto({
 required this.id,
 required this.email,
 this.phone,
 required this.name,
 required this.role,
 });

 factory UserDto.fromJson(Map<String, dynamic> json) => _$UserDtoFromJson(json);
}
```

### Verification Models

```dart
import 'package:json_annotation/json_annotation.dart';

part 'verification_models.g.dart';

@JsonSerializable()
class SendOtpRequest {
 final String phoneNumber; // backend expects PhoneNumber
 SendOtpRequest({required this.phoneNumber});
 Map<String, dynamic> toJson() => _$SendOtpRequestToJson(this);
}

@JsonSerializable()
class VerifyOtpRequest {
 final String phoneNumber; // PhoneNumber
 final String otpCode; // OtpCode
 VerifyOtpRequest({required this.phoneNumber, required this.otpCode});
 Map<String, dynamic> toJson() => _$VerifyOtpRequestToJson(this);
}
```

### Business Models

```dart
import 'package:json_annotation/json_annotation.dart';

part 'business_models.g.dart';

@JsonSerializable()
class BusinessDto {
 final int id;
 final int ownerId;
 final String? ownerName;
 final String name;
 final String? logoUrl;
 final String? coverPhotoUrl;
 final String? description;
 final String? category;
 final String? address;
 final double? latitude;
 final double? longitude;
 final String status;
 final int reviewCount;
 final double averageRating;
 final int followerCount;
 final DateTime createdAt;
 final DateTime? updatedAt;

 BusinessDto({
 required this.id,
 required this.ownerId,
 this.ownerName,
 required this.name,
 this.logoUrl,
 this.coverPhotoUrl,
 this.description,
 this.category,
 this.address,
 this.latitude,
 this.longitude,
 required this.status,
 required this.reviewCount,
 required this.averageRating,
 required this.followerCount,
 required this.createdAt,
 this.updatedAt,
 });

 factory BusinessDto.fromJson(Map<String, dynamic> json) => _$BusinessDtoFromJson(json);
}

@JsonSerializable()
class CreateBusinessRequest {
 final String name;
 final String? logoUrl;
 final String? coverPhotoUrl;
 final String? description;
 final String? category;
 final String? address;
 final double? latitude;
 final double? longitude;

 CreateBusinessRequest({
 required this.name,
 this.logoUrl,
 this.coverPhotoUrl,
 this.description,
 this.category,
 this.address,
 this.latitude,
 this.longitude,
 });

 Map<String, dynamic> toJson() => _$CreateBusinessRequestToJson(this);
}

@JsonSerializable()
class CreateReviewRequest {
 final int rating;
 final String? comment;
 final String? photoUrl;
 CreateReviewRequest({required this.rating, this.comment, this.photoUrl});
 Map<String, dynamic> toJson() => _$CreateReviewRequestToJson(this);
}

@JsonSerializable()
class BusinessInsights {
 final int followerCount;
 final int reviewCount;
 final double averageRating;
 final int postCount;

 BusinessInsights({
 required this.followerCount,
 required this.reviewCount,
 required this.averageRating,
 required this.postCount,
 });

 factory BusinessInsights.fromJson(Map<String, dynamic> json) => _$BusinessInsightsFromJson(json);
}
```

### Notification Models

```dart
import 'package:json_annotation/json_annotation.dart';

part 'notification_models.g.dart';

@JsonSerializable()
class NotificationDto {
 final int id;
 final String type;
 final String title;
 final String message;
 final int? relatedBusinessId;
 final String? relatedBusinessName;
 final bool isRead;
 final DateTime createdAt;

 NotificationDto({
 required this.id,
 required this.type,
 required this.title,
 required this.message,
 this.relatedBusinessId,
 this.relatedBusinessName,
 required this.isRead,
 required this.createdAt,
 });

 factory NotificationDto.fromJson(Map<String, dynamic> json) => _$NotificationDtoFromJson(json);
}

@JsonSerializable()
class UnreadCountResponse {
 final int count;
 UnreadCountResponse({required this.count});
 factory UnreadCountResponse.fromJson(Map<String, dynamic> json) => _$UnreadCountResponseFromJson(json);
}
```

---

## Retrofit API Service (envelope-aware)

Each endpoint returns `ApiResponse<T>` where `T` is the specific payload type. For endpoints with no payload, use `ApiResponse<Empty>`.

```dart
import 'package:dio/dio.dart';
import 'package:retrofit/retrofit.dart';
import '../models/auth_models.dart';
import '../models/business_models.dart';
import '../models/notification_models.dart';
import '../models/verification_models.dart';
import '../models/api_envelope.dart';

part 'api_service.g.dart';

@RestApi(baseUrl: "https://api.linkoja.com")
abstract class ApiService {
 factory ApiService(Dio dio, {String baseUrl}) = _ApiService;

 // Auth
 @POST("/api/auth/register")
 Future<ApiResponse<AuthResponse>> register(@Body() RegisterRequest request);

 @POST("/api/auth/login")
 Future<ApiResponse<AuthResponse>> login(@Body() LoginRequest request);

 @POST("/api/auth/social-login")
 Future<ApiResponse<AuthResponse>> socialLogin(@Body() SocialLoginRequest request);

 @POST("/api/auth/forgot-password")
 Future<ApiResponse<Map<String, dynamic>?>> forgotPassword(@Body() Map<String, dynamic> body);
 // body: { "email": "..." }, returns data: { token? } in dev

 @POST("/api/auth/reset-password")
 Future<ApiResponse<Empty>> resetPassword(@Body() Map<String, dynamic> body);

 @POST("/api/auth/change-password")
 Future<ApiResponse<Empty>> changePassword(@Body() Map<String, dynamic> body);

 // Verification
 @POST("/api/verification/send-otp")
 Future<ApiResponse<Empty>> sendOtp(@Body() SendOtpRequest request);

 @POST("/api/verification/verify-otp")
 Future<ApiResponse<Map<String, dynamic>?>> verifyOtp(@Body() VerifyOtpRequest request);

 @POST("/api/verification/resend-otp")
 Future<ApiResponse<Empty>> resendOtp(@Body() SendOtpRequest request);

 // Business
 @GET("/api/business")
 Future<ApiResponse<List<BusinessDto>>> getBusinesses(
 @Query("category") String? category,
 @Query("latitude") double? latitude,
 @Query("longitude") double? longitude,
 @Query("radiusKm") double? radiusKm,
 );

 @GET("/api/business/{id}")
 Future<ApiResponse<BusinessDto>> getBusinessById(@Path("id") int id);

 @POST("/api/business")
 Future<ApiResponse<Map<String, dynamic>>> createBusiness(@Body() CreateBusinessRequest request);
 // server returns entity; map to your model if needed

 @PUT("/api/business/{id}")
 Future<ApiResponse<Map<String, dynamic>>> updateBusiness(
 @Path("id") int id,
 @Body() CreateBusinessRequest request,
 );

 @DELETE("/api/business/{id}")
 Future<ApiResponse<Empty>> deleteBusiness(@Path("id") int id);

 @GET("/api/business/my-businesses")
 Future<ApiResponse<List<Map<String, dynamic>>>> getMyBusinesses();

 @GET("/api/business/{id}/insights")
 Future<ApiResponse<BusinessInsights>> getBusinessInsights(@Path("id") int id);

 // Reviews
 @POST("/api/business/{id}/reviews")
 Future<ApiResponse<Map<String, dynamic>>> addReview(
 @Path("id") int businessId,
 @Body() CreateReviewRequest request,
 );

 @POST("/api/business/reviews/{reviewId}/report")
 Future<ApiResponse<Empty>> reportReview(
 @Path("reviewId") int reviewId,
 @Body() Map<String, dynamic> body,
 );

 // Follow
 @POST("/api/business/{id}/follow")
 Future<ApiResponse<Empty>> followBusiness(@Path("id") int id);

 @DELETE("/api/business/{id}/follow")
 Future<ApiResponse<Empty>> unfollowBusiness(@Path("id") int id);

 // Notifications
 @GET("/api/notification")
 Future<ApiResponse<List<NotificationDto>>> getNotifications(
 @Query("unreadOnly") bool? unreadOnly,
 );

 @GET("/api/notification/unread-count")
 Future<ApiResponse<UnreadCountResponse>> getUnreadCount();

 @PUT("/api/notification/{id}/read")
 Future<ApiResponse<Empty>> markAsRead(@Path("id") int id);

 @PUT("/api/notification/read-all")
 Future<ApiResponse<Empty>> markAllAsRead();

 // Admin
 @GET("/api/admin/businesses/pending")
 Future<ApiResponse<List<Map<String, dynamic>>>> getPendingBusinesses();

 @POST("/api/admin/businesses/{id}/approve")
 Future<ApiResponse<Map<String, dynamic>>> approveBusiness(
 @Path("id") int id,
 @Body() Map<String, dynamic> body,
 );

 @GET("/api/admin/analytics")
 Future<ApiResponse<Map<String, dynamic>>> getAdminAnalytics();

 @GET("/api/admin/reports/reviews")
 Future<ApiResponse<List<Map<String, dynamic>>>> getReviewReports(
 @Query("status") String? status,
 );

 @PUT("/api/admin/reports/reviews/{reportId}/resolve")
 Future<ApiResponse<Empty>> resolveReviewReport(
 @Path("reportId") int reportId,
 @Query("action") String action,
 );
}
```

Notes:
- You can replace `Map<String, dynamic>` with concrete models as needed.
- For endpoints that return the created/updated entity, align with your app models.

---

## Error Handling

Update your error handler to understand the envelope even on error responses:

```dart
import 'package:dio/dio.dart';

class AppException implements Exception {
 final String message;
 final int? statusCode;
 AppException(this.message, {this.statusCode});
 @override String toString() => message;
}

class ErrorHandler {
 static AppException handleError(dynamic error) {
 if (error is DioException) {
 final status = error.response?.statusCode;
 final data = error.response?.data;
 final description = data is Map
 ? (data['response']?['description'] ?? data['message'] ?? 'Something went wrong')
 : 'Something went wrong';
 switch (status) {
 case400: return AppException(description, statusCode:400);
 case401: return AppException(description, statusCode:401);
 case404: return AppException(description, statusCode:404);
 case500: return AppException('Server error. Please try again later.', statusCode:500);
 default: return AppException(description, statusCode: status);
 }
 }
 return AppException(error.toString());
 }
}
```

---

## Usage Examples

```dart
// Example: Login
final res = await apiService.login(LoginRequest(email: 'a@b.com', password: 'pass'));
if (res.isSuccessful && res.response.code == '00') {
 final token = res.response.data!.token;
 final user = res.response.data!.user;
 // save token, proceed
} else {
 // show res.response.description
}
```

```dart
// Example: Get Businesses
final res = await apiService.getBusinesses(null, null, null, null);
if (res.isSuccessful) {
 final list = res.response.data ?? [];
}
```

---

## CORS

The backend sends CORS headers and handles OPTIONS preflight requests. No client changes are required for CORS.

---

## Next Steps

1. Add the new envelope models and regenerate code: `flutter pub run build_runner build --delete-conflicting-outputs`
2. Update repository methods to use `ApiResponse<T>` and check `isSuccessful` and `response.code`
3. Replace any legacy `MessageResponse`/non-envelope parsing with the new envelope parsing
4. Align request/response models to match backend property names as shown above
