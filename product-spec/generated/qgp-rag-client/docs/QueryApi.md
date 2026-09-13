# Qgp.Rag.Client.Api.QueryApi

All URIs are relative to *http://qgp-rag.internal:8100/v1*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**Query**](QueryApi.md#query) | **POST** /query | Hỏi–đáp RAG có trích nguồn (BOT-F-01/03/04) |

<a id="query"></a>
# **Query**
> QueryResponse Query (QueryRequest queryRequest, string? xRequestId = null)

Hỏi–đáp RAG có trích nguồn (BOT-F-01/03/04)

Retrieval chỉ trên corpus Effective đã index. Nếu không đủ căn cứ để trích nguồn → trả status=insufficient (answer=null) và ghi log câu hỏi (BOT-F-06). Không bao giờ trả answer khẳng định mà thiếu citations. 

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
    public class QueryExample
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
            var apiInstance = new QueryApi(httpClient, config, httpClientHandler);
            var queryRequest = new QueryRequest(); // QueryRequest | 
            var xRequestId = "xRequestId_example";  // string? | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) (optional) 

            try
            {
                // Hỏi–đáp RAG có trích nguồn (BOT-F-01/03/04)
                QueryResponse result = apiInstance.Query(queryRequest, xRequestId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling QueryApi.Query: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the QueryWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Hỏi–đáp RAG có trích nguồn (BOT-F-01/03/04)
    ApiResponse<QueryResponse> response = apiInstance.QueryWithHttpInfo(queryRequest, xRequestId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling QueryApi.QueryWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **queryRequest** | [**QueryRequest**](QueryRequest.md) |  |  |
| **xRequestId** | **string?** | Correlation id để trace xuyên .NET ↔ Python (OpenTelemetry, ADR-0013) | [optional]  |

### Return type

[**QueryResponse**](QueryResponse.md)

### Authorization

[serviceToken](../README.md#serviceToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Kết quả (answered với citations, hoặc insufficient) |  -  |
| **400** | Payload không hợp lệ |  -  |
| **401** | Thiếu/không hợp lệ service token |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

