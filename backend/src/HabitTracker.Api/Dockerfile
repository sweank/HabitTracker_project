FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY HabitTracker.Api.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV DATA_FILE=/app/data/habittracker.json
COPY --from=build /app/publish .
RUN mkdir -p /app/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "HabitTracker.Api.dll"]
