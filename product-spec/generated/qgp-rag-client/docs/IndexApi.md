# Qgp.Rag.Client.Api.IndexApi

All URIs are relative to *http://qgp-rag.internal:8100/v1*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**DeleteIndex**](IndexApi.md#deleteindex) | **DELETE** /index/{doc_id} | Gỡ tài liệu khỏi index (khi Superseded/Retired — BR-06) |
| [**UpsertIndex**](IndexApi.md#upsertindex) | **POST** /index/upsert | Index / cập nhật một tài liệu Effective (BOT-F-05) |

<a id="deleteindex"></a>
# **DeleteIndex**
> void DeleteIndex (string docId, string? xRequestId = null)

Gỡ tài liệu khỏi index (khi Superseded/Retired — BR-06)

qgp-api gọi khi tài liệu không còn Effective (bị supersede hoặc retire).

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
    public class DeleteIndexExample
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
            var apiInstance = new IndexApi(httpClient, config, httpClientHandler);
            var docId = "docId_example";  // string | 
            var xRequestId = "xRequestId_example";  // string? | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) (optional) 

            try
            {
                // Gỡ tài liệu khỏi index (khi Superseded/Retired — BR-06)
                apiInstance.DeleteIndex(docId, xRequestId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling IndexApi.DeleteIndex: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DeleteIndexWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Gỡ tài liệu khỏi index (khi Superseded/Retired — BR-06)
    apiInstance.DeleteIndexWithHttpInfo(docId, xRequestId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling IndexApi.DeleteIndexWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **docId** | **string** |  |  |
| **xRequestId** | **string?** | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) | [optional]  |

### Return type

void (empty response body)

### Authorization

[serviceToken](../README.md#serviceToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **204** | Đã gỡ (idempotent — không lỗi nếu chưa tồn tại) |  -  |
| **400** | Lỗi (client/nghiệp vụ) — xem code trong Error |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="upsertindex"></a>
# **UpsertIndex**
> UpsertIndex200Response UpsertIndex (UpsertRequest upsertRequest, string? xRequestId = null)

Index / cập nhật một tài liệu Effective (BOT-F-05)

qgp-api gọi khi một version trở thành Effective (WF-03) hoặc khi reindex. Idempotent theo doc_id — upsert THAY THẾ toàn bộ chunk của doc_id đó (đảm bảo chỉ 1 bản Effective được phản ánh, BR-02/BR-06). 

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
    public class UpsertIndexExample
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
            var apiInstance = new IndexApi(httpClient, config, httpClientHandler);
            var upsertRequest = new UpsertRequest(); // UpsertRequest | 
            var xRequestId = "xRequestId_example";  // string? | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) (optional) 

            try
            {
                // Index / cập nhật một tài liệu Effective (BOT-F-05)
                UpsertIndex200Response result = apiInstance.UpsertIndex(upsertRequest, xRequestId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling IndexApi.UpsertIndex: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the UpsertIndexWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Index / cập nhật một tài liệu Effective (BOT-F-05)
    ApiResponse<UpsertIndex200Response> response = apiInstance.UpsertIndexWithHttpInfo(upsertRequest, xRequestId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling IndexApi.UpsertIndexWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **upsertRequest** | [**UpsertRequest**](UpsertRequest.md) |  |  |
| **xRequestId** | **string?** | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) | [optional]  |

### Return type

[**UpsertIndex200Response**](UpsertIndex200Response.md)

### Authorization

[serviceToken](../README.md#serviceToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Đã index |  -  |
| **400** | Payload không hợp lệ |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

