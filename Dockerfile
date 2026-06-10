FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker cache for restore
COPY ["Productmanagement.sln", "./"]
COPY ["src/API/Productmanagement.API.csproj", "src/API/"]
COPY ["src/Application/ProductManagement.Application.csproj", "src/Application/"]
COPY ["src/Domain/ProductManagement.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/ProductManagement.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["tests/Application.Tests/ProductManagement.Application.Tests.csproj", "tests/Application.Tests/"]
COPY ["tests/API.Tests/API.Tests.csproj", "tests/API.Tests/"]
COPY ["tests/Infrastructure.Tests/Infrastructure.Tests.csproj", "tests/Infrastructure.Tests/"]

RUN dotnet restore "Productmanagement.sln"

# Copy the remaining files and build the application
COPY . .
WORKDIR "/src/src/API"
RUN dotnet build "Productmanagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Productmanagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the ASP.NET Core 8.0 runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Productmanagement.API.dll"]
