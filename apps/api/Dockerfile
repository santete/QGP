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
