# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files and restore
COPY EventSplit.Domain/EventSplit.Domain.csproj EventSplit.Domain/
COPY EventSplit.Application/EventSplit.Application.csproj EventSplit.Application/
COPY EventSplit.Infrastructure/EventSplit.Infrastructure.csproj EventSplit.Infrastructure/
COPY EventSplit.WebAPI/EventSplit.WebAPI.csproj EventSplit.WebAPI/
RUN dotnet restore EventSplit.WebAPI/EventSplit.WebAPI.csproj

# Copy source and publish
COPY . .
RUN dotnet publish EventSplit.WebAPI/EventSplit.WebAPI.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Install timezone data for SE Asia Standard Time
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh

COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EventSplit.WebAPI.dll"]
