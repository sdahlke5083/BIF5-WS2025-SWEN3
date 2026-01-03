using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Paperless.REST.BLL.Search;

public class MyElasticSearchClient
{
    private readonly ElasticsearchClient _esClient;
    private readonly string _indexName = "document_texts";

    public MyElasticSearchClient(IConfiguration configuration)
    {
        var host = configuration?.GetValue<string>("ELASTICSEARCH_HOST") ?? "elasticsearch";
        var port = configuration?.GetValue<string>("ELASTICSEARCH_PORT") ?? "9200";
        var baseUrl = $"http://{host}:{port}";

        var settings = new ElasticsearchClientSettings(new Uri(baseUrl));
        _esClient = new ElasticsearchClient(settings);

        // fire-and-forget ensure index
        _ = EnsureIndexAsync();
    }

    private async Task EnsureIndexAsync()
    {
        try
        {
            var exists = await _esClient.Indices.ExistsAsync(_indexName);
            if (!exists.Exists)
            {
                // create index with default mapping (dynamic)
                await _esClient.Indices.CreateAsync(_indexName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MyElasticSearchClient: could not ensure index: {ex.Message}");
        }
    }

    public async Task IndexDocumentTextAsync(Guid documentId, string ocrText, string? summary)
    {
        var doc = new
        {
            documentId = documentId,
            ocr = ocrText,
            summary = summary,
            timestamp = DateTime.UtcNow
        };

        var req = new IndexRequest<object>(doc, _indexName, documentId.ToString());
        await _esClient.IndexAsync(req);
    }

    public async Task UpdateSummaryAsync(Guid documentId, string summary)
    {
        var updateReq = new UpdateRequest<object, object>(_indexName, documentId.ToString())
        {
            Doc = new { summary = summary, timestamp = DateTime.UtcNow },
            DocAsUpsert = true
        };

        await _esClient.UpdateAsync(updateReq);
    }

    public async Task<List<Guid>> SearchAsync(string query, int from = 0, int size = 20)
    {
        var resp = await _esClient.SearchAsync<object>(s => s
            .Index(_indexName)
            .From(from)
            .Size(size)
            .Query(q => q.QueryString(qs => qs.Query($"*{query}*")))
        );

        if (!resp.IsValidResponse)
        {
            throw new InvalidOperationException("Elasticsearch search failed");
        }

        var results = new List<Guid>();
        foreach (var hit in resp.Hits)
        {
            if (Guid.TryParse(hit.Id, out var gid)) results.Add(gid);
        }
        return results;
    }
    public sealed class DocumentTextIndexEntry
    {
        [JsonPropertyName("documentId")]
        public Guid DocumentId { get; set; }

        [JsonPropertyName("ocr")]
        public string? Ocr { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public sealed class DocumentTextSearchHit
    {
        public Guid DocumentId { get; init; }
        public string? Ocr { get; init; }
        public string? Summary { get; init; }
        public DateTime Timestamp { get; init; }
    }

    public async Task<List<DocumentTextSearchHit>> SearchWithTextAsync(string query, int from = 0, int size = 20)
    {
        var resp = await _esClient.SearchAsync<DocumentTextIndexEntry>(s => s
            .Index(_indexName)
            .From(from)
            .Size(size)
            .Query(q => q.QueryString(qs => qs.Query($"*{query}*")))
        );

        if (!resp.IsValidResponse)
            throw new InvalidOperationException("Elasticsearch search failed");

        var hits = new List<DocumentTextSearchHit>();
        foreach (var h in resp.Hits)
        {
            if (!Guid.TryParse(h.Id, out var gid))
                continue;

            hits.Add(new DocumentTextSearchHit
            {
                DocumentId = gid,
                Ocr = h.Source?.Ocr,
                Summary = h.Source?.Summary,
                Timestamp = h.Source?.Timestamp ?? default
            });
        }

        return hits;
    }

    public async Task<DocumentTextIndexEntry?> GetTextByIdAsync(Guid documentId)
    {
        var resp = await _esClient.GetAsync<DocumentTextIndexEntry>(_indexName, documentId.ToString());
        if (!resp.Found) 
            return null;
        return resp.Source;
    }
}
