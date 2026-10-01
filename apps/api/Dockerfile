# QGP API (.NET 8) — multi-stage, non-root. Build context = apps/api.
# Build:  docker build -t qgp-api apps/api
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Restore tách lớp (cache theo csproj) trước khi copy toàn bộ source.
COPY src/Qgp.Api/Qgp.Api.csproj src/Qgp.Api/
RUN dotnet restore src/Qgp.Api/Qgp.Api.csproj
COPY src/ src/
RUN dotnet publish src/Qgp.Api/Qgp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# EF migration bundle (dotnet-ef pin ở .config/dotnet-tools.json). Production KHÔNG auto-migrate lúc
# app start → chạy image target 'migrator' một lần trước 'api' (service 'migrate' trong compose prod).
FROM build AS bundle
COPY .config/ .config/
RUN dotnet tool restore \
 && dotnet ef migrations bundle --project src/Qgp.Api/Qgp.Api.csproj --configuration Release -o /app/efbundle

# Build: docker build --target migrator -t qgp-migrator apps/api
# Conn: env QGP_DB_CONNECTION, không có thì đọc file /run/secrets/QGP_DB_CONNECTION (Docker secrets).
# Bundle dùng QgpDbContextFactory (đọc env QGP_DB_CONNECTION) — không đưa conn lên argv.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS migrator
WORKDIR /app
COPY --from=bundle /app/efbundle .
USER app
ENTRYPOINT ["/bin/sh", "-c", "export QGP_DB_CONNECTION=\"${QGP_DB_CONNECTION:-$(cat /run/secrets/QGP_DB_CONNECTION)}\" && exec ./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Git content store (ADR-0015) — thư mục bền cho content-repo, user 'app' (uid 1654) ghi được.
RUN mkdir -p /data/content-repo && chown -R app:app /data
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    QGP_GIT_REPO_PATH=/data/content-repo
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Qgp.Api.dll"]
