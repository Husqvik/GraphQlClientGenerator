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

    private static async Task<GraphQlSchema> QuerySchemaMetadata(HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content =
            response.Content is null
                ? "(no content)"
                : await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Status code: {(int)response.StatusCode} ({response.StatusCode}); content:{Environment.NewLine}{content}");

        return DeserializeGraphQlSchema(content);
    }

    public static async Task<GraphQlSchema> RetrieveSchema(
        HttpMethod method,
        string url,
        ICollection<KeyValuePair<string, string>> headers = null,
        HttpMessageHandler messageHandler = null,
        GraphQlWellKnownDirective? wellKnownDirectives = null,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient(messageHandler);

        if (wellKnownDirectives is null)
        {
            var schema = await QuerySchemaMetadata(httpClient, SetupHttpRequest(method, url, GraphQlIntrospection.QuerySupportedDirectives, headers), cancellationToken);
            wellKnownDirectives = schema.Directives.Any(d => d.Name is "oneOf") ? GraphQlWellKnownDirective.OneOf : GraphQlWellKnownDirective.None;
        }

        using var request = SetupHttpRequest(method, url, GraphQlIntrospection.QuerySchemaMetadata(wellKnownDirectives.Value), headers);
        return await QuerySchemaMetadata(httpClient, request, cancellationToken);
    }

    public static GraphQlSchema DeserializeGraphQlSchema(string contentJson)
    {
        try
        {
            var schema =
                JsonConvert.DeserializeObject<GraphQlResult>(contentJson, SerializerSettings)?.Data?.Schema
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
}