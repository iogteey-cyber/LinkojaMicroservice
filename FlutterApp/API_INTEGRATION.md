# Linkoja Mobile App - API Integration Guide

This guide provides complete implementation details for integrating the Flutter mobile app with the LinkojaMicroservice backend API.

## Table of Contents

1. [API Service Setup](#api-service-setup)
2. [Authentication & Token Management](#authentication--token-management)
3. [Data Models](#data-models)
4. [Repository Pattern](#repository-pattern)
5. [Error Handling](#error-handling)

---

## API Service Setup

### 1. Dio Client with Interceptors

**File: `lib/core/utils/dio_client.dart`**

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
            connectTimeout: const Duration(seconds: 30),
            receiveTimeout: const Duration(seconds: 30),
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json',
            },
          ),
        ) {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          // Add JWT token to all requests
          final token = await _storage.read(key: 'auth_token');
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          print('REQUEST[${options.method}] => PATH: ${options.path}');
          return handler.next(options);
        },
        onResponse: (response, handler) {
          print(
              'RESPONSE[${response.statusCode}] => PATH: ${response.requestOptions.path}');
          return handler.next(response);
        },
        onError: (DioException error, handler) async {
          print(
              'ERROR[${error.response?.statusCode}] => PATH: ${error.requestOptions.path}');
          
          // Handle 401 Unauthorized
          if (error.response?.statusCode == 401) {
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

### 2. Retrofit API Service

**File: `lib/data/services/api_service.dart`**

```dart
import 'package:dio/dio.dart';
import 'package:retrofit/retrofit.dart';
import '../models/auth_models.dart';
import '../models/business_models.dart';
import '../models/notification_models.dart';

part 'api_service.g.dart';

@RestApi(baseUrl: "https://api.linkoja.com")
abstract class ApiService {
  factory ApiService(Dio dio, {String baseUrl}) = _ApiService;

  // ========== AUTH ENDPOINTS ==========
  
  @POST("/api/auth/register")
  Future<AuthResponse> register(@Body() RegisterRequest request);

  @POST("/api/auth/login")
  Future<AuthResponse> login(@Body() LoginRequest request);

  @POST("/api/auth/social-login")
  Future<AuthResponse> socialLogin(@Body() SocialLoginRequest request);

  @POST("/api/auth/forgot-password")
  Future<MessageResponse> forgotPassword(@Body() ForgotPasswordRequest request);

  @POST("/api/auth/reset-password")
  Future<MessageResponse> resetPassword(@Body() ResetPasswordRequest request);

  @POST("/api/auth/change-password")
  Future<MessageResponse> changePassword(@Body() ChangePasswordRequest request);

  // ========== VERIFICATION ENDPOINTS ==========
  
  @POST("/api/verification/send-otp")
  Future<OtpResponse> sendOtp(@Body() SendOtpRequest request);

  @POST("/api/verification/verify-otp")
  Future<OtpVerifyResponse> verifyOtp(@Body() VerifyOtpRequest request);

  @POST("/api/verification/resend-otp")
  Future<OtpResponse> resendOtp(@Body() ResendOtpRequest request);

  // ========== BUSINESS ENDPOINTS ==========
  
  @GET("/api/business")
  Future<List<BusinessDto>> getBusinesses(
    @Query("category") String? category,
    @Query("latitude") double? latitude,
    @Query("longitude") double? longitude,
    @Query("radiusKm") double? radiusKm,
  );

  @GET("/api/business/{id}")
  Future<BusinessDto> getBusinessById(@Path("id") int id);

  @POST("/api/business")
  Future<BusinessDto> createBusiness(@Body() CreateBusinessRequest request);

  @PUT("/api/business/{id}")
  Future<BusinessDto> updateBusiness(
    @Path("id") int id,
    @Body() UpdateBusinessRequest request,
  );

  @DELETE("/api/business/{id}")
  Future<void> deleteBusiness(@Path("id") int id);

  @GET("/api/business/my-businesses")
  Future<List<BusinessDto>> getMyBusinesses();

  @GET("/api/business/{id}/insights")
  Future<BusinessInsights> getBusinessInsights(@Path("id") int id);

  // ========== REVIEW ENDPOINTS ==========
  
  @POST("/api/business/{id}/reviews")
  Future<ReviewDto> addReview(
    @Path("id") int businessId,
    @Body() CreateReviewRequest request,
  );

  @POST("/api/business/reviews/{reviewId}/report")
  Future<MessageResponse> reportReview(
    @Path("reviewId") int reviewId,
    @Body() ReportReviewRequest request,
  );

  // ========== FOLLOW ENDPOINTS ==========
  
  @POST("/api/business/{id}/follow")
  Future<MessageResponse> followBusiness(@Path("id") int id);

  @DELETE("/api/business/{id}/follow")
  Future<MessageResponse> unfollowBusiness(@Path("id") int id);

  // ========== POST ENDPOINTS ==========
  
  @POST("/api/business/{id}/posts")
  Future<PostDto> createPost(
    @Path("id") int businessId,
    @Body() CreatePostRequest request,
  );

  // ========== NOTIFICATION ENDPOINTS ==========
  
  @GET("/api/notification")
  Future<List<NotificationDto>> getNotifications(
    @Query("unreadOnly") bool? unreadOnly,
  );

  @GET("/api/notification/unread-count")
  Future<UnreadCountResponse> getUnreadCount();

  @PUT("/api/notification/{id}/read")
  Future<void> markAsRead(@Path("id") int id);

  @PUT("/api/notification/read-all")
  Future<void> markAllAsRead();

  // ========== ADMIN ENDPOINTS ==========
  
  @GET("/api/admin/businesses/pending")
  Future<List<BusinessDto>> getPendingBusinesses();

  @POST("/api/admin/businesses/{id}/approve")
  Future<MessageResponse> approveBusiness(
    @Path("id") int id,
    @Body() ApproveBusinessRequest request,
  );

  @GET("/api/admin/analytics")
  Future<AdminAnalytics> getAdminAnalytics();

  @GET("/api/admin/reports/reviews")
  Future<List<ReviewReportDto>> getReviewReports(
    @Query("status") String? status,
  );

  @PUT("/api/admin/reports/reviews/{reportId}/resolve")
  Future<MessageResponse> resolveReviewReport(
    @Path("reportId") int reportId,
    @Query("action") String action,
  );
}
```

### 3. Generate Code

Run code generation:

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

---

## Authentication & Token Management

### JWT Token Storage

**File: `lib/data/services/storage_service.dart`**

```dart
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class StorageService {
  final FlutterSecureStorage _storage;

  StorageService(this._storage);

  // Auth Token
  Future<void> saveAuthToken(String token) async {
    await _storage.write(key: 'auth_token', value: token);
  }

  Future<String?> getAuthToken() async {
    return await _storage.read(key: 'auth_token');
  }

  Future<void> deleteAuthToken() async {
    await _storage.delete(key: 'auth_token');
  }

  // User Data
  Future<void> saveUserId(int userId) async {
    await _storage.write(key: 'user_id', value: userId.toString());
  }

  Future<int?> getUserId() async {
    final id = await _storage.read(key: 'user_id');
    return id != null ? int.tryParse(id) : null;
  }

  // Clear All Data
  Future<void> clearAll() async {
    await _storage.deleteAll();
  }
}
```

---

## Data Models

### Auth Models

**File: `lib/data/models/auth_models.dart`**

```dart
import 'package:json_annotation/json_annotation.dart';

part 'auth_models.g.dart';

// Register Request
@JsonSerializable()
class RegisterRequest {
  final String email;
  final String password;
  final String phoneNumber;
  final String fullName;

  RegisterRequest({
    required this.email,
    required this.password,
    required this.phoneNumber,
    required this.fullName,
  });

  Map<String, dynamic> toJson() => _$RegisterRequestToJson(this);
}

// Login Request
@JsonSerializable()
class LoginRequest {
  final String email;
  final String password;

  LoginRequest({required this.email, required this.password});

  Map<String, dynamic> toJson() => _$LoginRequestToJson(this);
}

// Social Login Request
@JsonSerializable()
class SocialLoginRequest {
  final String provider;
  final String idToken;

  SocialLoginRequest({required this.provider, required this.idToken});

  Map<String, dynamic> toJson() => _$SocialLoginRequestToJson(this);
}

// Auth Response
@JsonSerializable()
class AuthResponse {
  final String token;
  final UserDto user;

  AuthResponse({required this.token, required this.user});

  factory AuthResponse.fromJson(Map<String, dynamic> json) =>
      _$AuthResponseFromJson(json);
}

// User DTO
@JsonSerializable()
class UserDto {
  final int id;
  final String email;
  final String fullName;
  final String? phoneNumber;
  final bool isPhoneVerified;
  final String role;
  final String? profilePictureUrl;

  UserDto({
    required this.id,
    required this.email,
    required this.fullName,
    this.phoneNumber,
    required this.isPhoneVerified,
    required this.role,
    this.profilePictureUrl,
  });

  factory UserDto.fromJson(Map<String, dynamic> json) =>
      _$UserDtoFromJson(json);
}

// OTP Requests
@JsonSerializable()
class SendOtpRequest {
  final String phoneNumber;

  SendOtpRequest({required this.phoneNumber});

  Map<String, dynamic> toJson() => _$SendOtpRequestToJson(this);
}

@JsonSerializable()
class VerifyOtpRequest {
  final String phoneNumber;
  final String code;

  VerifyOtpRequest({required this.phoneNumber, required this.code});

  Map<String, dynamic> toJson() => _$VerifyOtpRequestToJson(this);
}

@JsonSerializable()
class OtpResponse {
  final String message;

  OtpResponse({required this.message});

  factory OtpResponse.fromJson(Map<String, dynamic> json) =>
      _$OtpResponseFromJson(json);
}
```

### Business Models

**File: `lib/data/models/business_models.dart`**

```dart
import 'package:json_annotation/json_annotation.dart';

part 'business_models.g.dart';

@JsonSerializable()
class BusinessDto {
  final int id;
  final String name;
  final String? logoUrl;
  final String? coverPhotoUrl;
  final String description;
  final String category;
  final String address;
  final double? latitude;
  final double? longitude;
  final String? phoneNumber;
  final String? email;
  final String? website;
  final String status;
  final int ownerId;
  final double averageRating;
  final int reviewCount;
  final int followerCount;
  final DateTime createdAt;

  BusinessDto({
    required this.id,
    required this.name,
    this.logoUrl,
    this.coverPhotoUrl,
    required this.description,
    required this.category,
    required this.address,
    this.latitude,
    this.longitude,
    this.phoneNumber,
    this.email,
    this.website,
    required this.status,
    required this.ownerId,
    required this.averageRating,
    required this.reviewCount,
    required this.followerCount,
    required this.createdAt,
  });

  factory BusinessDto.fromJson(Map<String, dynamic> json) =>
      _$BusinessDtoFromJson(json);
}

@JsonSerializable()
class CreateBusinessRequest {
  final String name;
  final String? logoUrl;
  final String? coverPhotoUrl;
  final String description;
  final String category;
  final String address;
  final double latitude;
  final double longitude;
  final String? phoneNumber;
  final String? email;
  final String? website;

  CreateBusinessRequest({
    required this.name,
    this.logoUrl,
    this.coverPhotoUrl,
    required this.description,
    required this.category,
    required this.address,
    required this.latitude,
    required this.longitude,
    this.phoneNumber,
    this.email,
    this.website,
  });

  Map<String, dynamic> toJson() => _$CreateBusinessRequestToJson(this);
}

@JsonSerializable()
class ReviewDto {
  final int id;
  final int businessId;
  final int userId;
  final String userName;
  final int rating;
  final String comment;
  final String? photoUrl;
  final DateTime createdAt;

  ReviewDto({
    required this.id,
    required this.businessId,
    required this.userId,
    required this.userName,
    required this.rating,
    required this.comment,
    this.photoUrl,
    required this.createdAt,
  });

  factory ReviewDto.fromJson(Map<String, dynamic> json) =>
      _$ReviewDtoFromJson(json);
}

@JsonSerializable()
class CreateReviewRequest {
  final int rating;
  final String comment;
  final String? photoUrl;

  CreateReviewRequest({
    required this.rating,
    required this.comment,
    this.photoUrl,
  });

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

  factory BusinessInsights.fromJson(Map<String, dynamic> json) =>
      _$BusinessInsightsFromJson(json);
}
```

---

## Repository Pattern

### Auth Repository

**File: `lib/data/repositories/auth_repository.dart`**

```dart
import '../services/api_service.dart';
import '../models/auth_models.dart';

class AuthRepository {
  final ApiService _apiService;

  AuthRepository(this._apiService);

  Future<AuthResponse> register(
    String email,
    String password,
    String phoneNumber,
    String fullName,
  ) async {
    final request = RegisterRequest(
      email: email,
      password: password,
      phoneNumber: phoneNumber,
      fullName: fullName,
    );
    return await _apiService.register(request);
  }

  Future<AuthResponse> login(String email, String password) async {
    final request = LoginRequest(email: email, password: password);
    return await _apiService.login(request);
  }

  Future<AuthResponse> socialLogin(String provider, String idToken) async {
    final request = SocialLoginRequest(provider: provider, idToken: idToken);
    return await _apiService.socialLogin(request);
  }

  Future<void> sendOtp(String phoneNumber) async {
    final request = SendOtpRequest(phoneNumber: phoneNumber);
    await _apiService.sendOtp(request);
  }

  Future<void> verifyOtp(String phoneNumber, String code) async {
    final request = VerifyOtpRequest(phoneNumber: phoneNumber, code: code);
    await _apiService.verifyOtp(request);
  }

  Future<void> resendOtp(String phoneNumber) async {
    final request = ResendOtpRequest(phoneNumber: phoneNumber);
    await _apiService.resendOtp(request);
  }

  Future<void> forgotPassword(String email) async {
    final request = ForgotPasswordRequest(email: email);
    await _apiService.forgotPassword(request);
  }

  Future<void> resetPassword(String token, String newPassword) async {
    final request =
        ResetPasswordRequest(token: token, newPassword: newPassword);
    await _apiService.resetPassword(request);
  }

  Future<void> changePassword(String oldPassword, String newPassword) async {
    final request = ChangePasswordRequest(
      oldPassword: oldPassword,
      newPassword: newPassword,
    );
    await _apiService.changePassword(request);
  }
}
```

### Business Repository

**File: `lib/data/repositories/business_repository.dart`**

```dart
import '../services/api_service.dart';
import '../models/business_models.dart';

class BusinessRepository {
  final ApiService _apiService;

  BusinessRepository(this._apiService);

  Future<List<BusinessDto>> getBusinesses({
    String? category,
    double? latitude,
    double? longitude,
    double? radiusKm,
  }) async {
    return await _apiService.getBusinesses(
      category,
      latitude,
      longitude,
      radiusKm,
    );
  }

  Future<BusinessDto> getBusinessById(int id) async {
    return await _apiService.getBusinessById(id);
  }

  Future<BusinessDto> createBusiness(CreateBusinessRequest request) async {
    return await _apiService.createBusiness(request);
  }

  Future<BusinessDto> updateBusiness(
    int id,
    UpdateBusinessRequest request,
  ) async {
    return await _apiService.updateBusiness(id, request);
  }

  Future<void> deleteBusiness(int id) async {
    await _apiService.deleteBusiness(id);
  }

  Future<List<BusinessDto>> getMyBusinesses() async {
    return await _apiService.getMyBusinesses();
  }

  Future<BusinessInsights> getBusinessInsights(int id) async {
    return await _apiService.getBusinessInsights(id);
  }

  Future<ReviewDto> addReview(
    int businessId,
    CreateReviewRequest request,
  ) async {
    return await _apiService.addReview(businessId, request);
  }

  Future<void> reportReview(int reviewId, String reason, String description) async {
    final request = ReportReviewRequest(reason: reason, description: description);
    await _apiService.reportReview(reviewId, request);
  }

  Future<void> followBusiness(int id) async {
    await _apiService.followBusiness(id);
  }

  Future<void> unfollowBusiness(int id) async {
    await _apiService.unfollowBusiness(id);
  }

  Future<PostDto> createPost(int businessId, CreatePostRequest request) async {
    return await _apiService.createPost(businessId, request);
  }
}
```

---

## Error Handling

### Custom Exception Classes

**File: `lib/core/exceptions/app_exceptions.dart`**

```dart
class AppException implements Exception {
  final String message;
  final int? statusCode;

  AppException(this.message, {this.statusCode});

  @override
  String toString() => message;
}

class NetworkException extends AppException {
  NetworkException(String message) : super(message);
}

class UnauthorizedException extends AppException {
  UnauthorizedException(String message) : super(message, statusCode: 401);
}

class NotFoundException extends AppException {
  NotFoundException(String message) : super(message, statusCode: 404);
}

class ValidationException extends AppException {
  final Map<String, List<String>>? errors;

  ValidationException(String message, {this.errors})
      : super(message, statusCode: 400);
}
```

### Error Handler Utility

**File: `lib/core/utils/error_handler.dart`**

```dart
import 'package:dio/dio.dart';
import '../exceptions/app_exceptions.dart';

class ErrorHandler {
  static AppException handleError(dynamic error) {
    if (error is DioException) {
      switch (error.type) {
        case DioExceptionType.connectionTimeout:
        case DioExceptionType.sendTimeout:
        case DioExceptionType.receiveTimeout:
          return NetworkException('Connection timeout. Please check your internet connection.');
        
        case DioExceptionType.badResponse:
          return _handleResponseError(error.response);
        
        case DioExceptionType.cancel:
          return AppException('Request was cancelled');
        
        default:
          return NetworkException('No internet connection');
      }
    }
    
    return AppException(error.toString());
  }

  static AppException _handleResponseError(Response? response) {
    final statusCode = response?.statusCode;
    final data = response?.data;

    switch (statusCode) {
      case 400:
        return ValidationException(
          data['message'] ?? 'Validation error',
          errors: data['errors'],
        );
      case 401:
        return UnauthorizedException(
          data['message'] ?? 'Unauthorized. Please login again.',
        );
      case 404:
        return NotFoundException(
          data['message'] ?? 'Resource not found',
        );
      case 500:
        return AppException(
          'Server error. Please try again later.',
          statusCode: 500,
        );
      default:
        return AppException(
          data['message'] ?? 'Something went wrong',
          statusCode: statusCode,
        );
    }
  }
}
```

---

## Usage Examples

### Making an API Call

```dart
// In your provider or repository
try {
  final businesses = await businessRepository.getBusinesses(
    category: 'Food',
    latitude: 6.5244,
    longitude: 3.3792,
    radiusKm: 5.0,
  );
  
  // Handle success
  print('Found ${businesses.length} businesses');
} on AppException catch (e) {
  // Handle specific app exceptions
  print('Error: ${e.message}');
} catch (e) {
  // Handle unexpected errors
  print('Unexpected error: $e');
}
```

### With Provider State Management

```dart
class BusinessProvider with ChangeNotifier {
  final BusinessRepository _repository;
  
  List<BusinessDto> _businesses = [];
  bool _isLoading = false;
  String? _error;

  Future<void> loadBusinesses({
    String? category,
    double? latitude,
    double? longitude,
    double? radiusKm,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _businesses = await _repository.getBusinesses(
        category: category,
        latitude: latitude,
        longitude: longitude,
        radiusKm: radiusKm,
      );
    } on AppException catch (e) {
      _error = e.message;
    } catch (e) {
      _error = 'An unexpected error occurred';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
```

---

## Next Steps

1. Generate model files: `flutter pub run build_runner build`
2. Test API calls with real backend
3. Implement all repository methods
4. Add comprehensive error handling
5. Test with different network conditions

For complete screen implementations, see `FEATURES.md`.
