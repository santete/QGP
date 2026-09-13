# Qgp.Rag.Client.Model.QueryResponse

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Status** | **string** |  | 
**Answer** | **string** |  | [optional] 
**Citations** | [**List&lt;QueryResponseCitationsInner&gt;**](QueryResponseCitationsInner.md) | bắt buộc non-empty khi status&#x3D;answered (BOT-F-03) | [optional] 
**UnansweredLogged** | **bool** | true khi insufficient và đã log (BOT-F-06) | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

