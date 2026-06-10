# Product Management API

A secure, production-grade RESTful backend API for managing products and items with role-based access control, JWT authentication, and complete CRUD operations.

## Project Description

The **Product Management API** is a backend system built with ASP.NET Core 8 that enables organizations to manage their product catalog through a set of well-defined RESTful endpoints. The API supports creating, reading, updating, and deleting products (each with associated items), with secure access controls that distinguish between regular users and administrators.

**Real-world use case:** An e-commerce or inventory management platform where administrators manage product catalogs while authenticated users browse available products.

### Key Highlights

- Clean Architecture with separation of concerns across four layers
- Secure authentication using JWT access tokens (HttpOnly cookies) with refresh token rotation
- Role-based authorization (Admin/User) for granular access control
- Automatic API documentation via Swagger/OpenAPI
- Docker and Docker Compose support for containerized deployment
- Comprehensive validation, error handling, and logging

## Features

- **CRUD Operations** — Full create, read, update, and delete support for products and items
- **JWT Authentication** — Secure token-based authentication with HttpOnly cookies
- **Refresh Token Rotation** — Automatic token rotation with compromised-token detection
- **Role-Based Access Control** — Admin-only policies for write operations
- **Input Validation** — FluentValidation rules executed via a global action filter
- **Global Exception Handling** — Middleware that maps exceptions to standardized API responses
- **Structured Logging** — Built-in logging across all service layers
- **API Documentation** — Swagger UI available in development mode
- **Pagination** — Page-based pagination with configurable page size
- **Repository Pattern** — Generic repositories with Unit of Work for data access
- **Clean Architecture** — Domain, Application, Infrastructure, and API layers
- **Dependency Injection** — Built-in ASP.NET Core DI throughout
- **Entity Framework Core** — Code-first approach with SQL Server
- **Docker Support** — Multi-stage Dockerfile and Docker Compose setup
- **API Versioning** — URL-segment versioning (`/api/v1/`)
- **Health Checks** — `/health` endpoint for monitoring
- **CORS** — Configurable cross-origin support for SPA clients
- **AutoMapper** — Automated mapping between entities and DTOs
- **AsNoTracking** — Performance optimization for read-only queries

## Tech Stack

| Technology | Purpose |
|---|---|
| **ASP.NET Core 8 Web API** | API framework |
| **C# 12** | Programming language |
| **Entity Framework Core 8** | ORM for data access |
| **SQL Server 2022** | Relational database |
| **JWT (JSON Web Tokens)** | Authentication |
| **Swagger / Swashbuckle** | API documentation |
| **AutoMapper** | Object-to-object mapping |
| **FluentValidation** | Request validation |
| **BCrypt.Net** | Password hashing |
| **xUnit + Moq** | Unit testing |
| **Docker / Docker Compose** | Containerization |
| **ASP.NET Core API Versioning** | API version management |

## Project Structure

```
ProductManagement.sln
├── src/
│   ├── API/                              # ASP.NET Core Web API host
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs         # Authentication endpoints
│   │   │   └── ProductsController.cs     # Product CRUD endpoints
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs  # DI registration
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs       # Global validation filter
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs  # Global error handler
│   │   ├── Program.cs                    # Application entry point
│   │   └── appsettings.json              # Configuration
│   │
│   ├── Application/                      # Business logic layer
│   │   ├── Common/
│   │   │   └── Exceptions/
│   │   │       ├── NotFoundException.cs
│   │   │       └── AuthenticationException.cs
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── AuthResponse.cs
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   └── RegisterRequest.cs
│   │   │   └── Product/
│   │   │       ├── CreateItemRequest.cs
│   │   │       ├── CreateProductRequest.cs
│   │   │       ├── ItemDto.cs
│   │   │       ├── ProductDto.cs
│   │   │       ├── ProductQueryParameters.cs
│   │   │       └── UpdateProductRequest.cs
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs
│   │   │   ├── IJwtService.cs
│   │   │   └── IProductService.cs
│   │   ├── Mapping/
│   │   │   └── MappingProfile.cs         # AutoMapper configuration
│   │   ├── Responses/
│   │   │   ├── ApiResponse.cs            # Unified response envelope
│   │   │   └── PagedResponse.cs          # Paginated response wrapper
│   │   ├── Services/
│   │   │   ├── AuthService.cs            # Authentication business logic
│   │   │   └── ProductService.cs         # Product business logic
│   │   └── Validators/
│   │       ├── CreateProductRequestValidator.cs
│   │       ├── LoginRequestValidator.cs
│   │       ├── ProductQueryParametersValidator.cs
│   │       ├── RegisterRequestValidator.cs
│   │       └── UpdateProductRequestValidator.cs
│   │
│   ├── Domain/                           # Enterprise core layer
│   │   ├── Entities/
│   │   │   ├── Item.cs
│   │   │   ├── Product.cs
│   │   │   ├── RefreshToken.cs
│   │   │   └── User.cs
│   │   ├── Enums/
│   │   │   └── Role.cs                   # Admin, User
│   │   └── Interfaces/
│   │       ├── IRepository.cs            # Generic repository contract
│   │       └── ... (specific repository interfaces)
│   │
│   └── Infrastructure/                   # Data access & external concerns
│       ├── Data/
│       │   ├── ApplicationDbContext.cs   # EF Core DbContext
│       │   ├── Configurations/           # Entity type configurations
│       │   ├── Repositories/             # Repository implementations
│       │   └── UnitOfWork.cs             # Transaction coordination
│       ├── Identity/
│       │   └── JwtService.cs             # JWT token generation
│       └── Logging/                      # Logging infrastructure
│
├── tests/
│   ├── Application.Tests/                # Unit tests for services
│   ├── API.Tests/                        # Integration tests
│   └── Infrastructure.Tests/             # Infrastructure layer tests
│
├── Dockerfile                            # Multi-stage Docker build
├── docker-compose.yml                    # API + SQL Server orchestration
└── Productmanagement.sln                 # Solution file
```

## API Endpoints

### Authentication

| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| POST | `/api/v1/auth/register` | Register a new user | No |
| POST | `/api/v1/auth/login` | Log in with email and password | No |
| POST | `/api/v1/auth/refresh-token` | Refresh access token using refresh token | Yes (Cookie) |
| POST | `/api/v1/auth/logout` | Revoke refresh token and clear cookies | Yes (Cookie) |

### Products

| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/api/v1/products?pageNumber=1&pageSize=20` | Get paginated list of products | No |
| GET | `/api/v1/products/{id}` | Get a product by ID | No |
| GET | `/api/v1/products/{id}/items` | Get all items for a product | No |
| POST | `/api/v1/products` | Create a new product with items | Admin |
| PUT | `/api/v1/products/{id}` | Update an existing product | Admin |
| DELETE | `/api/v1/products/{id}` | Delete a product and its items | Admin |

### Health

| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/health` | Application health check | No |

### Response Format

All endpoints return a consistent JSON envelope:

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": { ... },
  "errors": null
}
```

Paginated responses wrap data in a `PagedResponse`:

```json
{
  "success": true,
  "message": "Products retrieved successfully.",
  "data": {
    "items": [ ... ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalItems": 50,
    "totalPages": 3
  },
  "errors": null
}
```

## Authentication Flow

The API uses a **JWT + Refresh Token** strategy with HttpOnly cookies for secure token storage:

1. **Registration / Login** — The client sends credentials to `/auth/register` or `/auth/login`. Upon success, the server generates an access token (short-lived, 15 minutes) and a refresh token (long-lived, 7 days). Both tokens are written to HttpOnly, SameSite=Strict cookies (`accessToken` and `refreshToken`).

2. **Authenticated Requests** — The `JwtBearerMiddleware` reads the `accessToken` cookie automatically. No `Authorization` header is needed — the cookie is sent with every request.

3. **Token Refresh** — When the access token expires, the client calls `/auth/refresh-token`. The server validates the refresh token, issues a new access token and a new refresh token, and rotates the old refresh token (revokes it). If a revoked refresh token is reused, all active sessions for that user are immediately revoked (compromised-token protection).

4. **Logout** — The client calls `/auth/logout`, which revokes the refresh token server-side and clears both cookies.

5. **Role-Based Authorization** — The `[Authorize(Policy = "AdminOnly")]` attribute on write endpoints ensures only users with the `Admin` role can create, update, or delete products. The first registered user is automatically assigned the `Admin` role; subsequent registrations default to `User`.

## Database

### Database Engine

- **SQL Server** (LocalDB for development, SQL Server 2022 container for Docker)

### Entity Framework Core Migrations

The application applies pending migrations automatically at startup via `context.Database.MigrateAsync()` in `Program.cs`. To manage migrations manually:

```bash
# Navigate to the Infrastructure project directory
cd src/Infrastructure

# Add a new migration
dotnet ef migrations add InitialCreate --startup-project ../API/Productmanagement.API.csproj

# Apply migrations to the database
dotnet ef database update --startup-project ../API/Productmanagement.API.csproj
```

### Database Schema

```
Product
├── Id (int, PK, identity)
├── ProductName (nvarchar 255, not null)
├── CreatedBy (nvarchar 100, not null)
├── CreatedOn (datetime, not null)
├── ModifiedBy (nvarchar 100, nullable)
├── ModifiedOn (datetime, nullable)
└── Items (navigation collection)

Item
├── Id (int, PK, identity)
├── ProductId (int, FK → Product.Id, cascade delete)
├── Quantity (int, not null)
└── Product (navigation property)

User
├── Id (guid, PK)
├── Username (nvarchar, not null)
├── Email (nvarchar, not null)
├── PasswordHash (nvarchar, not null)
├── Role (int, enum: User=0, Admin=1)
├── CreatedAt (datetime, not null)
└── RefreshTokens (navigation collection)

RefreshToken
├── Id (guid, PK)
├── Token (nvarchar, not null)
├── Expires (datetime, not null)
├── CreatedAt (datetime, not null)
├── CreatedByIp (nvarchar, not null)
├── RevokedAt (datetime, nullable)
├── RevokedByIp (nvarchar, nullable)
├── ReplacedByToken (nvarchar, nullable)
├── UserId (guid, FK → User.Id)
└── User (navigation property)
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Developer Edition, or container)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for containerized deployment)

### Running Locally

```bash
# Clone the repository
git clone <repository-url>
cd Productmanagement

# Restore dependencies
dotnet restore

# Run the application (uses LocalDB by default)
dotnet run --project src/API/Productmanagement.API.csproj
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`). Swagger UI is available at `https://localhost:5001/swagger`.

### Running with Docker

```bash
# Build and start containers (API + SQL Server)
docker-compose up --build

# The API will be available at http://localhost:8080
# Swagger UI at http://localhost:8080/swagger
```

### Running Tests

```bash
dotnet test Productmanagement.sln
```

## Configuration

Key settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProductManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "YOUR_STRONG_SECRET_KEY_HERE",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

> **Important:** Change the `JwtSettings:Secret` to a strong, unique value in production. Never commit secrets to source control.

Environment variables override settings when running in Docker (see `docker-compose.yml` for details).

## Security

- **HttpOnly cookies** — Tokens are inaccessible to JavaScript, mitigating XSS attacks
- **SameSite=Strict** — Prevents CSRF attacks by restricting cookie sending to same-site requests
- **Secure flag** — Cookies are marked Secure in production (require HTTPS)
- **Refresh Token Rotation** — Each refresh invalidates the previous token; reuse detection revokes all sessions
- **Password Hashing** — Passwords are hashed with BCrypt before storage
- **Input Validation** — All inputs are validated with FluentValidation rules
- **Global Error Handling** — Exceptions are caught by middleware that returns sanitized responses (no stack trace leakage in production)
