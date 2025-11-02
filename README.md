# Linkoja Microservice

A comprehensive ASP.NET Core microservice for managing businesses, users, reviews, and social features for the Linkoja platform.

## Features

- **User Authentication**: JWT-based authentication with secure password hashing using BCrypt
- **Business Management**: Create, update, and manage business profiles
- **Reviews & Ratings**: Users can review and rate businesses
- **Social Features**: Follow businesses and create posts
- **RESTful API**: Well-structured API endpoints with Swagger documentation
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
│   └── BusinessController.cs
├── Data/                # Database context
│   └── ApplicationDbContext.cs
├── DTOs/                # Data Transfer Objects
│   ├── AuthDtos.cs
│   └── BusinessDtos.cs
├── Models/              # Entity models
│   ├── User.cs
│   ├── Business.cs
│   ├── BusinessReview.cs
│   ├── BusinessFollower.cs
│   ├── BusinessPost.cs
│   └── BusinessCategory.cs
├── Services/            # Business logic
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── IBusinessService.cs
│   └── BusinessService.cs
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

#### Authentication

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and receive JWT token

#### Business Management

- `GET /api/business` - Get all businesses (with optional filters)
- `GET /api/business/{id}` - Get business by ID
- `POST /api/business` - Create a new business (requires authentication)
- `PUT /api/business/{id}` - Update business (requires authentication)
- `DELETE /api/business/{id}` - Delete business (requires authentication)
- `GET /api/business/my-businesses` - Get current user's businesses (requires authentication)

#### Reviews & Social Features

- `POST /api/business/{id}/reviews` - Add a review (requires authentication)
- `POST /api/business/{id}/follow` - Follow a business (requires authentication)
- `DELETE /api/business/{id}/follow` - Unfollow a business (requires authentication)
- `POST /api/business/{id}/posts` - Create a post for a business (requires authentication)

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
