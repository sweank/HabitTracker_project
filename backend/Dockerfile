FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/HabitTracker.Domain/HabitTracker.Domain.csproj src/HabitTracker.Domain/
COPY src/HabitTracker.Application/HabitTracker.Application.csproj src/HabitTracker.Application/
COPY src/HabitTracker.Infrastructure/HabitTracker.Infrastructure.csproj src/HabitTracker.Infrastructure/
COPY src/HabitTracker.Api/HabitTracker.Api.csproj src/HabitTracker.Api/
COPY tests/HabitTracker.Tests/HabitTracker.Tests.csproj tests/HabitTracker.Tests/

RUN dotnet restore tests/HabitTracker.Tests/HabitTracker.Tests.csproj
RUN dotnet restore src/HabitTracker.Api/HabitTracker.Api.csproj

COPY . .

# Restore once more after copying sources. This protects the build even if stale
# obj/project.assets.json files were accidentally copied into the context.
RUN dotnet restore tests/HabitTracker.Tests/HabitTracker.Tests.csproj
RUN dotnet restore src/HabitTracker.Api/HabitTracker.Api.csproj

RUN dotnet test tests/HabitTracker.Tests/HabitTracker.Tests.csproj -c Release --no-restore
RUN dotnet publish src/HabitTracker.Api/HabitTracker.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HabitTracker.Api.dll"]
