using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GraphQlClientGenerator;

public static class GraphQlHttpUtilities
{
    public static HttpClientHandler CreateDefaultHttpClientHandler() =>
        new() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };

    private static HttpClient CreateHttpClient(HttpMessageHandler messageHandler = null) =>
        new(messageHandler ?? CreateDefaultHttpClientHandler())
        {
            DefaultRequestHeaders =
            {
                UserAgent = { ProductInfoHeaderValue.Parse($"GraphQlClientGenerator/{typeof(GraphQlGenerator).GetTypeInfo().Assembly.GetName().Version}") }
            }
        };

    private static readonly JsonSerializerSettings SerializerSettings =
        new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() }
        };

    private static HttpRequestMessage SetupHttpRequest(HttpMethod method, string url, string queryText, IEnumerable<KeyValuePair<string, string>> headers)
    {
        var request = new HttpRequestMessage(method, url);
        if (request.Method == HttpMethod.Get)
            request.RequestUri = new($"{request.RequestUri}?&query={queryText}");
        else
            request.Content = new StringContent(JsonConvert.SerializeObject(new { query = queryText }), Encoding.UTF8, "application/json");

        if (headers is not null)
            foreach (var kvp in headers)
                request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);

        return request;
    }

    private static async Task<string> QuerySchemaMetadata(HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content =
            response.Content is null
                ? "(no content)"
                : await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Status code: {(int)response.StatusCode} ({response.StatusCode}); content:{Environment.NewLine}{content}");

        return content;
    }

    public static async Task<GraphQlSchema> RetrieveSchema(
        HttpMethod method,
        string url,
        ICollection<KeyValuePair<string, string>> headers = null,
        HttpMessageHandler messageHandler = null,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient(messageHandler);

        var typeMetadataJson = await QuerySchemaMetadata(httpClient, SetupHttpRequest(method, url, GraphQlIntrospection.QuerySupportedFieldArgs, headers), cancellationToken);
        var typeMetadata = JsonConvert.DeserializeObject<GraphQlResult<GraphQlTypeMetadata>>(typeMetadataJson).Data?.TypeMetadata;
        var queryParameters = GetMetadataQueryParameters(typeMetadata);

        using var request = SetupHttpRequest(method, url, GraphQlIntrospection.QuerySchemaMetadata(queryParameters.HasOneOfSupport, queryParameters.HasDeprecatedInputFieldSupport), headers);
        return DeserializeGraphQlSchema(await QuerySchemaMetadata(httpClient, request, cancellationToken));
    }

    public static GraphQlSchema DeserializeGraphQlSchema(string contentJson)
    {
        try
        {
            var schema =
                JsonConvert.DeserializeObject<GraphQlResult<GraphQlData>>(contentJson, SerializerSettings)?.Data?.Schema
                ?? JsonConvert.DeserializeObject<GraphQlData>(contentJson, SerializerSettings)?.Schema;

            return schema ?? throw new ArgumentException(NotGraphQlSchemaMessage(contentJson));
        }
        catch (JsonReaderException exception)
        {
            throw new ArgumentException(NotGraphQlSchemaMessage(contentJson), exception);
        }
    }

    private static string NotGraphQlSchemaMessage(string content) =>
        $"""
         not a GraphQL schema:
         {content}
         """;

    private static MetadataQueryParameters GetMetadataQueryParameters(GraphQlType typeMetadata) =>
        new()
        {
            HasOneOfSupport = typeMetadata?.Fields.Any(f => f.Name is "isOneOf") ?? false,
            HasDeprecatedInputFieldSupport =
                typeMetadata?.Fields
                    .Where(f => f.Name is "inputFields")
                    .Any(f => f.Args?.Any(a => a.Name is "includeDeprecated") ?? false)
                ?? false
        };

    private class GraphQlTypeMetadata
    {
        public GraphQlType TypeMetadata { get; set; }
    }

    private struct MetadataQueryParameters
    {
        public bool HasOneOfSupport { get; set; }
        public bool HasDeprecatedInputFieldSupport { get; set; }
    }
}