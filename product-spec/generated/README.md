# Generated skeletons — QGP

Sinh từ OpenAPI (openapi-generator 7.x, Java 17). **Đã build sạch: 0 error / 0 warning** (.NET 8).
Đây là *skeleton* — team copy vào project thật rồi hiện thực phần `TODO` (business logic, DI, EF Core...).

## 1. `qgp-api-server/` — ASP.NET Core server stub (public API)
- Nguồn: `../api/openapi.yaml`
- Generator: `aspnetcore` (aspnetCoreVersion 8.0, package `Qgp.Api`, Swashbuckle)
- Nội dung: 8 controller theo tag (`Controllers/DocumentsApi.cs`, `VersionsApi.cs`, `SearchApi.cs`,
  `RecommendationsApi.cs`, `FeedbackApi.cs`, `ReportsApi.cs`, `AuditApi.cs`, `AssistantApi.cs`),
  31 model (`Models/`), `Program.cs`, `Startup.cs`, Swagger UI.
- Mỗi action là `virtual` với `//TODO:` — override để nối tầng nghiệp vụ (state machine, RBAC, EF Core).
- Method name = `operationId` (vd `PublishVersion`, `GetRecommendations`, `AskAssistant`).

Build/chạy:
```bash
dotnet build product-spec/generated/qgp-api-server/src/Qgp.Api/Qgp.Api.csproj
dotnet run   --project product-spec/generated/qgp-api-server/src/Qgp.Api   # Swagger UI ở /swagger
```

## 2. `qgp-rag-client/` — .NET client cho qgp-rag (ADR-0007)
- Nguồn: `../api/qgp-rag.openapi.yaml` (contract nội bộ .NET ↔ Python)
- Generator: `csharp` (library `httpclient`, net8.0, package `Qgp.Rag.Client`)
- Nội dung: `Api/QueryApi.cs` (query), `Api/IndexApi.cs` (upsertIndex/deleteIndex), `Api/OpsApi.cs`
  (reindex/health) + model (`QueryResponse`, `UpsertRequest`...).
- `qgp-api` dùng client này gọi service Python `qgp-rag` (query/index/reindex) — xem
  `../api/rag-integration-contract.md`.

Ví dụ dùng trong qgp-api:
```csharp
var rag = new QueryApi(new Configuration { BasePath = "http://qgp-rag.internal:8100/v1" });
var res = rag.Query(new QueryRequest(question: "Quality Gate 1 la gi?"));
if (res.Status == QueryResponse.StatusEnum.Answered) { /* dùng res.Answer + res.Citations */ }
```

## Regenerate
```bash
npx @openapitools/openapi-generator-cli generate -g aspnetcore \
  -i product-spec/api/openapi.yaml -o product-spec/generated/qgp-api-server \
  --additional-properties=aspnetCoreVersion=8.0,packageName=Qgp.Api,buildTarget=program,useSwashbuckle=true,operationIsAsync=true

npx @openapitools/openapi-generator-cli generate -g csharp \
  -i product-spec/api/qgp-rag.openapi.yaml -o product-spec/generated/qgp-rag-client \
  --additional-properties=packageName=Qgp.Rag.Client,library=httpclient,targetFramework=net8.0
```

> Gợi ý bổ sung (khi cần): client **TypeScript** cho frontend React (`-g typescript-fetch` từ `openapi.yaml`);
> server stub **Python FastAPI** cho qgp-rag (`-g python-fastapi` từ `qgp-rag.openapi.yaml`).
