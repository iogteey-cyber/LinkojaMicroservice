# Linkoja Microservice

A comprehensive ASP.NET Core microservice for managing businesses, users, reviews, and social features for the Linkoja platform.

## Features

- **User Authentication**: JWT-based authentication with secure password hashing using BCrypt
- **Password Management**: Complete password reset flow with secure tokens
- **Business Management**: Create, update, and manage business profiles with verification workflow
- **Geolocation Search**: Find businesses near you with distance-based filtering
- **Reviews & Ratings**: Users can review and rate businesses (1-5 stars)
- **Social Features**: Follow businesses and create posts
- **Admin Dashboard**: Business approval workflow and platform analytics
- **Notification System**: Real-time notifications for followers, reviews, and approvals
- **Business Analytics**: Insights for business owners on performance metrics
- **RESTful API**: 27+ well-structured API endpoints with Swagger documentation
- **PostgreSQL Database**: Robust data persistence with Entity Framework Core

## Tech Stack

- **Framework**: ASP.NET Core 6.0
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Password Hashing**: BCrypt.Net
- **API Documentation**: Swagger/OpenAPI
- **ORM**: Entity Framework Core with Npgsql

## Project Structure

```
LinkojaMicroservice/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── BusinessController.cs
│   ├── AdminController.cs
│   └── NotificationController.cs
├── Data/                # Database context
│   └── ApplicationDbContext.cs
├── DTOs/                # Data Transfer Objects
│   ├── AuthDtos.cs
│   ├── BusinessDtos.cs
│   ├── AdminDtos.cs
│   ├── PasswordDtos.cs
│   └── NotificationDtos.cs
├── Models/              # Entity models
│   ├── User.cs
│   ├── Business.cs
│   ├── BusinessReview.cs
│   ├── BusinessFollower.cs
│   ├── BusinessPost.cs
│   ├── BusinessCategory.cs
│   ├── PasswordResetToken.cs
│   └── Notification.cs
├── Services/            # Business logic
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── IBusinessService.cs
│   ├── BusinessService.cs
│   ├── INotificationService.cs
│   └── NotificationService.cs
├── Program.cs
├── Startup.cs
└── appsettings.json
```

## Prerequisites

- .NET 6.0 SDK or later
- PostgreSQL 12 or later
- An IDE (Visual Studio, VS Code, or Rider)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/iogteey-cyber/LinkojaMicroservice.git
cd LinkojaMicroservice
```

### 2. Configure Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=linkoja_db;Username=your_username;Password=your_password"
  }
}
```

### 3. Update JWT Settings

For production, change the JWT key in `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "LinkojaMicroservice",
    "Audience": "LinkojaApp",
    "ExpiryMinutes": 60
  }
}
```

### 4. Install Dependencies

```bash
dotnet restore
```

### 5. Create Database

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 6. Run the Application

```bash
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000`.

## API Documentation

Once the application is running, access the Swagger UI at:
- https://localhost:5001/swagger

### Main Endpoints

#### Authentication (5 endpoints)

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and receive JWT token
- `POST /api/auth/forgot-password` - Request password reset token
- `POST /api/auth/reset-password` - Reset password with token
- `POST /api/auth/change-password` - Change password (requires authentication)

#### Business Management (13 endpoints)

- `GET /api/business` - Get all businesses (supports category, status, geolocation filters)
  - Query params: `?category=Food&latitude=6.5244&longitude=3.3792&radiusKm=5`
- `GET /api/business/{id}` - Get business by ID
- `POST /api/business` - Create a new business (requires authentication)
- `PUT /api/business/{id}` - Update business (requires authentication)
- `DELETE /api/business/{id}` - Delete business (requires authentication)
- `GET /api/business/my-businesses` - Get current user's businesses (requires authentication)
- `POST /api/business/{id}/reviews` - Add a review (requires authentication)
- `POST /api/business/{id}/follow` - Follow a business (requires authentication)
- `DELETE /api/business/{id}/follow` - Unfollow a business (requires authentication)
- `POST /api/business/{id}/posts` - Create a post for a business (requires authentication)
- `GET /api/business/{id}/insights` - Get business analytics (owner only)

#### Admin Dashboard (5 endpoints, requires admin role)

- `GET /api/admin/businesses/pending` - View pending business approvals
- `POST /api/admin/businesses/{id}/approve` - Approve or reject a business
- `GET /api/admin/analytics` - Get platform-wide analytics
- `GET /api/admin/businesses` - View all businesses with filters
- `DELETE /api/admin/businesses/{id}` - Delete a business

#### Notifications (4 endpoints, requires authentication)

- `GET /api/notification` - Get user notifications (supports `?unreadOnly=true`)
- `GET /api/notification/unread-count` - Get count of unread notifications
- `PUT /api/notification/{id}/read` - Mark notification as read
- `PUT /api/notification/read-all` - Mark all notifications as read

## Authentication

The API uses JWT Bearer tokens for authentication. To authenticate:

1. Register or login to receive a token
2. Include the token in the `Authorization` header:
   ```
   Authorization: Bearer <your-token>
   ```

In Swagger UI, click the "Authorize" button and enter: `Bearer <your-token>`

## Database Schema

### User
- Id, Email, Phone, PasswordHash, Name, Role, CreatedAt, UpdatedAt

### Business
- Id, OwnerId, Name, LogoUrl, CoverPhotoUrl, Description, Category, Address, Latitude, Longitude, Status, VerificationDocUrl, CreatedAt, UpdatedAt

### BusinessReview
- Id, BusinessId, UserId, Rating (1-5), Comment, CreatedAt, UpdatedAt

### BusinessFollower
- Id, BusinessId, UserId, FollowedAt

### BusinessPost
- Id, BusinessId, Content, ImageUrl, VideoUrl, Likes, Comments, CreatedAt, UpdatedAt

### BusinessCategory
- Id, BusinessId, CategoryName, CreatedAt

### PasswordResetToken
- Id, UserId, Token, ExpiresAt, IsUsed, CreatedAt

### Notification
- Id, UserId, Type, Title, Message, RelatedBusinessId, IsRead, CreatedAt

## Development

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Creating Migrations

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Security Considerations

- **Change the JWT secret key** in production
- Store sensitive configuration in **environment variables** or **Azure Key Vault**
- Use **HTTPS** in production
- Implement **rate limiting** for API endpoints
- Add **input validation** and **sanitization**
- Keep dependencies **up to date**

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is part of the Linkoja platform.

## Contact

Project Link: [https://github.com/iogteey-cyber/LinkojaMicroservice](https://github.com/iogteey-cyber/LinkojaMicroservice)
