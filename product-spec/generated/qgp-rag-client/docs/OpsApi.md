# Qgp.Rag.Client.Api.OpsApi

All URIs are relative to *http://qgp-rag.internal:8100/v1*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**Health**](OpsApi.md#health) | **GET** /healthz | Health check |
| [**Reindex**](OpsApi.md#reindex) | **POST** /reindex | Rebuild toàn bộ index (runbook RB-02) |

<a id="health"></a>
# **Health**
> Health200Response Health ()

Health check

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Qgp.Rag.Client.Api;
using Qgp.Rag.Client.Client;
using Qgp.Rag.Client.Model;

namespace Example
{
    public class HealthExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://qgp-rag.internal:8100/v1";
            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new OpsApi(httpClient, config, httpClientHandler);

            try
            {
                // Health check
                Health200Response result = apiInstance.Health();
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OpsApi.Health: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the HealthWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Health check
    ApiResponse<Health200Response> response = apiInstance.HealthWithHttpInfo();
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OpsApi.HealthWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters
This endpoint does not need any parameter.
### Return type

[**Health200Response**](Health200Response.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Lỗi (client/nghiệp vụ) — xem code trong Error |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="reindex"></a>
# **Reindex**
> void Reindex (ReindexRequest reindexRequest, string? xRequestId = null)

Rebuild toàn bộ index (runbook RB-02)

Nhận danh sách tài liệu Effective (nguồn sự thật do qgp-api cung cấp) và rebuild từ đầu. Dùng khi index lỗi/lệch. Chạy nền, idempotent. 

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Qgp.Rag.Client.Api;
using Qgp.Rag.Client.Client;
using Qgp.Rag.Client.Model;

namespace Example
{
    public class ReindexExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://qgp-rag.internal:8100/v1";
            // Configure Bearer token for authorization: serviceToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new OpsApi(httpClient, config, httpClientHandler);
            var reindexRequest = new ReindexRequest(); // ReindexRequest | 
            var xRequestId = "xRequestId_example";  // string? | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) (optional) 

            try
            {
                // Rebuild toàn bộ index (runbook RB-02)
                apiInstance.Reindex(reindexRequest, xRequestId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OpsApi.Reindex: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ReindexWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Rebuild toàn bộ index (runbook RB-02)
    apiInstance.ReindexWithHttpInfo(reindexRequest, xRequestId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OpsApi.ReindexWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **reindexRequest** | [**ReindexRequest**](ReindexRequest.md) |  |  |
| **xRequestId** | **string?** | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) | [optional]  |

### Return type

void (empty response body)

### Authorization

[serviceToken](../README.md#serviceToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **202** | Đã nhận, rebuild chạy nền |  -  |
| **400** | Lỗi (client/nghiệp vụ) — xem code trong Error |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

