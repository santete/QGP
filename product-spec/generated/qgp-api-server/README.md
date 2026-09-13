# Qgp.Api - ASP.NET Core 8.0 Server

Public REST API cho QGP (qgp-api, .NET 8). Sinh từ SDD-QGP-001 §3, map từ PRD-QGP-001 v1.0. Base path /v1. Auth Bearer (OIDC). Verb unsafe cần header Idempotency-Key. Error code theo ISC Internal Standard — API & Error Code.


## Upgrade NuGet Packages

NuGet packages get frequently updated.

To upgrade this solution to the latest version of all NuGet packages, use the dotnet-outdated tool.


Install dotnet-outdated tool:

```
dotnet tool install --global dotnet-outdated-tool
```

Upgrade only to new minor versions of packages

```
dotnet outdated --upgrade --version-lock Major
```

Upgrade to all new versions of packages (more likely to include breaking API changes)

```
dotnet outdated --upgrade
```


## Run

Linux/OS X:

```
sh build.sh
```

Windows:

```
build.bat
```
## Run in Docker

```
cd src/Qgp.Api
docker build -t qgp.api .
docker run -p 5000:8080 qgp.api
```
