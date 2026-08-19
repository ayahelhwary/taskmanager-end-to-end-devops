# ============================================
# Stage 1: Build 
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY src/TaskManagerApi/TaskManagerApi.csproj src/TaskManagerApi/
RUN dotnet restore src/TaskManagerApi/TaskManagerApi.csproj

COPY src/TaskManagerApi/ src/TaskManagerApi/

RUN dotnet publish src/TaskManagerApi/TaskManagerApi.csproj \
    -c Release \
    -o /app/publish 

# ============================================
# Stage 2: Runtime 
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TaskManagerApi.dll"]