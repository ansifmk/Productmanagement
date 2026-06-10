# RESTful Backend API Solution - Technical Assessment

## Overview

This document outlines the approach to implementing a RESTful backend API solution as requested in the technical assessment. The solution is designed with scalability, maintainability, clean architecture (layered structure), and industry best practices in mind.

## Problem Statement

Design a RESTful API solution around Products and Items to perform CRUD operations with secure access controls and structured validations.

## Test Submission

1. Do not submit/upload your code in this repository.
2. **_Create your own public repo_** and share the link with us.

---

## Architecture

A Clean, Layered Technical Architecture separating concerns across Domain, Application, Infrastructure, and API boundaries.

### Tech Stack

- **Framework**: .NET 8 with C#
- **API Framework**: ASP.NET Core Web API with ASP.NET Core API Versioning (v1)
- **Database**: SQL Server LocalDB / SQL Server Container using Entity Framework Core 8
- **Authentication**: JWT with HttpOnly cookies and Refresh Token rotation strategy
- **Testing**: xUnit and Moq (unit tests for application and authorization layers)
- **Documentation**: Swagger/OpenAPI with Swashbuckle
- **Containerization**: Docker and Docker Compose
- **Logging**: Microsoft.Extensions.Logging for structured logging

### Project Structure

```
Solution/
├── src/
│   ├── API/                  # ASP.NET Core Web API project (Productmanagement.API)
│   │   ├── Controllers/      # API controllers (v1/Products, v1/Auth)
│   │   ├── Filters/          # Action filters (ValidationFilter for automatic validation)
│   │   ├── Middleware/       # Custom middleware (Global exception handling)
│   │   ├── Extensions/       # DI registration extensions (ServiceCollectionExtensions)
│   │   ├── Program.cs        # Application entry point and pipeline configuration
│   │   └── appsettings.json  # Configuration files
│   ├── Application/          # Application logic layer (ProductManagement.Application)
│   │   ├── DTOs/             # Data Transfer Objects (Request/Response contracts)
│   │   ├── Interfaces/       # Service interfaces
│   │   ├── Mapping/          # AutoMapper mapping profiles (MappingProfile)
│   │   ├── Services/         # Service implementations (ProductService, AuthService)
│   │   └── Validators/       # FluentValidation request validation rules
│   ├── Domain/               # Domain layer (ProductManagement.Domain)
│   │   ├── Entities/         # Domain models (Product, Item, User, RefreshToken)
│   │   ├── Enums/            # Enumeration types (Role)
│   │   ├── Events/           # Domain events
│   │   └── Exceptions/       # Custom domain exceptions
│   └── Infrastructure/       # Infrastructure layer (ProductManagement.Infrastructure)
│       ├── Data/             # Data access components
│       │   ├── Configurations/  # EF Core entity configuration mappings
│       │   ├── Repositories/    # Repository implementations (ProductRepository, UserRepository)
│       │   ├── ApplicationDbContext.cs  # EF Core DbContext
│       │   └── UnitOfWork.cs    # Unit of Work implementation
│       ├── Identity/          # Authentication services (JwtService for token generation)
│       └── Logging/           # Logging infrastructure
├── tests/
│   ├── API.Tests/            # Integration tests placeholder project
│   ├── Application.Tests/    # Unit tests for Services & Auth (using xUnit and Moq)
│   └── Infrastructure.Tests/ # Unit tests for infrastructure layer
└── docker-compose.yml        # Docker Compose configuration
```

---

## API Design Expectation

### Resource-Oriented

- Resources are identified by clear, consistent URLs.
- Actions on resources are represented by standard HTTP methods.
- Resource relationships are reflected in the URL structure.

### Request/Response Format

- JSON is used for all request and response bodies.
- Consistent envelope response format (`ApiResponse<T>`):
  ```json
  {
    "success": true,
    "message": "Operation completed successfully.",
    "data": { ... },
    "errors": null
  }
  ```
- Standard HTTP status codes (200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 500 Internal Server Error) are used contextually.

### Endpoint Structure Example

- **Get Products (Paged)**: `GET /api/v1/products?pageNumber=1&pageSize=10` (Anonymous/User/Admin access)
- **Get Product by ID**: `GET /api/v1/products/{id}` (Anonymous/User/Admin access)
- **Get Product Items**: `GET /api/v1/products/{id}/items` (Anonymous/User/Admin access)
- **Create Product**: `POST /api/v1/products` (Admin access only)
- **Update Product**: `PUT /api/v1/products/{id}` (Admin access only)
- **Delete Product**: `DELETE /api/v1/products/{id}` (Admin access only)
- **Auth Endpoints**:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login`
  - `POST /api/v1/auth/refresh-token`
  - `POST /api/v1/auth/logout`

### Database Structure

```sql
CREATE TABLE [dbo].[Product]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY (1,1),
    [ProductName] NVARCHAR(255) NOT NULL,
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [CreatedOn] DATETIME NOT NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [ModifiedOn] DATETIME NULL
)

CREATE TABLE [dbo].[Item]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY (1,1),
    [ProductId] INT NOT NULL FOREIGN KEY REFERENCES Product(Id) ON DELETE CASCADE,
    [Quantity] INT NOT NULL
)
```

---

## Implementation Highlights

### Authentication & Authorization

- **JWT + Refresh Token Strategy**:
  - Validated access tokens are stored securely in the user's browser via an `accessToken` cookie.
  - Refresh tokens are rotation-tracked and stored in a `refreshToken` cookie.
  - Both cookies are configured as `HttpOnly`, `Secure` (in production), and `SameSite = Strict` to mitigate XSS and CSRF risks.
  - First registered user automatically receives the `Admin` role; subsequent sign-ups default to `User`.
  - Sensitive operations (`POST`, `PUT`, `DELETE` on products) are protected via the `[Authorize(Policy = "AdminOnly")]` attribute.

### Error Handling Middleware

- A global `ExceptionHandlingMiddleware` intercepts unhandled runtime exceptions.
- It translates exceptions (like `NotFoundException`, `AuthenticationException`, `ValidationException`) into appropriate HTTP status codes and maps them to a uniform error contract (`ApiResponse<object>`) to prevent leakage of internal stack traces.

### Data Validation with FluentValidation

- Implemented an automatic `ValidationFilter` (ActionFilter) registered globally in `Program.cs`.
- It executes FluentValidation check-rules on incoming request models (such as `CreateProductRequest` and `UpdateProductRequest`) before reaching the controller logic.
- If invalid, it immediately short-circuits the pipeline and throws a structured validation response.

### Controller Example with API Versioning

API Controllers are version-annotated using standard ASP.NET Core URL-segment versioning:
```csharp
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ProductDto>.SuccessResponse(result, "Product retrieved successfully"));
    }
}
```

### Service Layer Approach

- Business logic is completely isolated inside the **Application** project (`ProductService`, `AuthService`).
- Services operate solely on DTOs, preventing the leakage of domain models to the outer API controllers.

### Repository Pattern with Entity Framework Core

- Implemented a generic repository interface and implementation `IRepository<TEntity, TKey>` / `Repository<TEntity, TKey>` supporting both `Guid` and `int` key types.
- A `UnitOfWork` coordinate operations across different repositories (e.g. `Products`, `Items`, `Users`) to secure database transactions.

---

## Testing Strategy

### Unit Tests with xUnit and Moq

- In-depth unit tests verify core business workflows inside the `tests/Application.Tests` project:
  - **`ProductServiceTests.cs`**: Verifies product retrieval, creation, related items mapping, updates, and cascading hard deletion.
  - **`AuthServiceTests.cs`**: Validates registration, duplicate email checking, role assignments, secure logins, token cookie writing, and refresh token rotation/revocation logic.
- Run tests:
  ```bash
  dotnet test Productmanagement.sln
  ```

---

## Performance Considerations

- **AsNoTracking**: Read-only queries inside `GetPagedProductsAsync` use `.AsNoTracking()` to reduce EF Core tracking overhead.
- **Related Resource Loads**: Uses selective `.Include()` eager loading to fetch items and avoid $N+1$ select queries.
- **Pagination**: The collection endpoint supports query pagination (`pageNumber` and `pageSize`) to limit transfer payload sizes.
- **Thread Optimization**: Fully asynchronous calls (`async`/`await`) are utilized end-to-end to maximize resource utilization and scale under load.

---

## Security Measures

- Secure HttpOnly & SameSite Cookie flags for JWT storage to safeguard against token hijacking.
- Refresh Token Rotation: If a revoked or inactive refresh token is reused, all active tokens for that user are immediately invalidated (compromised token protection flow).
- Strong Password Hashing using `BCrypt.Net-Next`.
- Parameter validations on length and formats using FluentValidation.

---

## Deployment Configuration

### Docker Setup

#### Dockerfile
The application uses a multi-stage Docker build targeting .NET 8.0 SDK and ASP.NET Core Runtime:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Productmanagement.sln", "./"]
COPY ["src/API/Productmanagement.API.csproj", "src/API/"]
COPY ["src/Application/ProductManagement.Application.csproj", "src/Application/"]
COPY ["src/Domain/ProductManagement.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/ProductManagement.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["tests/Application.Tests/ProductManagement.Application.Tests.csproj", "tests/Application.Tests/"]
COPY ["tests/API.Tests/API.Tests.csproj", "tests/API.Tests/"]
COPY ["tests/Infrastructure.Tests/Infrastructure.Tests.csproj", "tests/Infrastructure.Tests/"]

RUN dotnet restore "Productmanagement.sln"

COPY . .
WORKDIR "/src/src/API"
RUN dotnet build "Productmanagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Productmanagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Productmanagement.API.dll"]
```

#### docker-compose.yml
The services setup runs the API container along with Microsoft SQL Server 2022:
```yaml
version: '3.8'

services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: product-db
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Password_Strong_123!
    ports:
      - "1433:1433"
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "Password_Strong_123!", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    image: product-management-api
    container_name: product-api
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    depends_on:
      db:
        condition: service_healthy
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db,1433;Database=ProductManagementDb;User Id=sa;Password=Password_Strong_123!;TrustServerCertificate=True;MultipleActiveResultSets=true
      - JwtSettings__Secret=THIS_IS_A_VERY_STRONG_SECRET_KEY_AND_YOU_SHOULD_CHANGE_IT_IN_PRODUCTION_ENVIRONMENT
      - JwtSettings__AccessTokenExpirationMinutes=15
      - JwtSettings__RefreshTokenExpirationDays=7
```
