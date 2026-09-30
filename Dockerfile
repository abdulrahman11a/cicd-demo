# ---------- Stage 1: build with the full SDK ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project files first so "dotnet restore" is cached until dependencies change
COPY CicdDemo.sln ./
COPY src/CicdDemo.Api/CicdDemo.Api.csproj src/CicdDemo.Api/
COPY tests/CicdDemo.Tests/CicdDemo.Tests.csproj tests/CicdDemo.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/CicdDemo.Api/CicdDemo.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: small runtime image (no SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
ARG GIT_SHA=dev
ENV GIT_SHA=$GIT_SHA

COPY --from=build /app/publish .

# Run as the non-root "app" user that ships with the .NET images
USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CicdDemo.Api.dll"]
