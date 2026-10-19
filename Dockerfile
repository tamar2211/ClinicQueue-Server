# שלב 1: בנייה עם ה-SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ClinicQueue.API/ClinicQueue.API.csproj -c Release -o /app

# שלב 2: הרצה עם runtime קל בלבד
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Render קובע את הפורט במשתנה PORT; מקומית ברירת המחדל היא 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet ClinicQueue.API.dll"]
