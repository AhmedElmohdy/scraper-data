using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using ElasticTest.Config;
using ElasticTest.Models;
using Microsoft.Extensions.Options;

namespace ElasticTest.Services;

public class ElasticService
{
    public ElasticsearchClient ProductsClient { get; }
    public string ProductsIndex { get; }

    // ?? Back-compat aliases so existing code keeps working
    private ElasticsearchClient _client => ProductsClient;
    private string _indexName => ProductsIndex;

    public ElasticService(IOptions<ElasticOptions> options)
    {
        var cfg = options.Value;

        var productSettings = new ElasticsearchClientSettings(new Uri(cfg.Products.Uri))
            .Authentication(new BasicAuthentication(cfg.Products.Username, cfg.Products.Password))
            .DefaultIndex(cfg.Products.Index);

        ProductsClient = new ElasticsearchClient(productSettings);
        ProductsIndex = cfg.Products.Index;
    }

    // ---------- Index bootstrapping ----------
    public async Task EnsureIndexAsync(CancellationToken ct = default)
    {
        var exists = await _client.Indices.ExistsAsync(_indexName, ct);
        if (exists.Exists) return;

        var createReq = new CreateIndexRequest(_indexName)
        {
            Settings = new IndexSettings
            {
                NumberOfShards = 1,
                NumberOfReplicas = 0
            },
            Mappings = new TypeMapping
            {
                Properties = new Properties
                {
                    { "id", new KeywordProperty() },
                    { "name", new TextProperty
                        {
                            Fields = new Properties
                            {
                                { "keyword", new KeywordProperty() }
                            }
                        }
                    },
                    { "description", new TextProperty() },
                    { "category", new KeywordProperty() },
                    { "price", new DoubleNumberProperty() },
                    { "stock", new IntegerNumberProperty() },
                    { "createdAt", new DateProperty() },
                    { "updatedAt", new DateProperty() }
                }
            }
        };

        var create = await _client.Indices.CreateAsync(createReq, ct);
        if (!create.IsValidResponse)
            throw new Exception($"Failed to create index '{_indexName}': {create.ElasticsearchServerError?.ToString() ?? create.DebugInformation}");
    }

    // ---------- CRUD ----------
    public async Task<Product?> GetAsync(string id, CancellationToken ct = default)
    {
        var resp = await _client.GetAsync<Product>(id, g => g.Index(_indexName), ct);
        return resp.Found ? resp.Source : null;
    }

    public async Task IndexAsync(Product product, CancellationToken ct = default)
    {
        product.UpdatedAt = DateTime.UtcNow;

        var req = new IndexRequest<Product>(product)
        {
            Index = _indexName,
            Id = product.Id
        };

        var resp = await _client.IndexAsync(req, ct);
        if (!resp.IsValidResponse) throw new Exception(resp.DebugInformation);
    }

    public async Task<bool> UpdatePartialAsync(string id, Action<Product> patch, CancellationToken ct = default)
    {
        var existing = await GetAsync(id, ct);
        if (existing is null) return false;

        patch(existing);
        existing.UpdatedAt = DateTime.UtcNow;

        var req = new IndexRequest<Product>(existing)
        {
            Index = _indexName,
            Id = id
        };

        var resp = await _client.IndexAsync(req, ct);
        return resp.IsValidResponse;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var resp = await _client.DeleteAsync<Product>(id, d => d.Index(_indexName), ct);
        return resp.IsValidResponse && resp.Result == Elastic.Clients.Elasticsearch.Result.Deleted;
    }

    // ---------- Search ----------
    public sealed record ProductSearchRequest(
        string? q,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        string? sortDir,
        int page = 1,
        int pageSize = 20);

    public sealed record PagedResult<T>(IReadOnlyCollection<T> items, long total, int page, int pageSize);

    public async Task<PagedResult<Product>> SearchAsync(ProductSearchRequest req, CancellationToken ct = default)
    {
        var must = new List<Query>();
        var filter = new List<Query>();

        if (!string.IsNullOrWhiteSpace(req.q))
        {
            must.Add(new MultiMatchQuery
            {
                Query = req.q,
                Fields = new[] { "name^3", "description" }
            });
        }

        if (!string.IsNullOrWhiteSpace(req.category))
        {
            filter.Add(new TermQuery
            {
                Field = new Field("category.keyword"),
                Value = req.category
            });
        }

        if (req.minPrice is not null || req.maxPrice is not null)
        {
            var range = new NumberRangeQuery(new Field("price"));
            if (req.minPrice is not null) range.Gte = (double?)req.minPrice;
            if (req.maxPrice is not null) range.Lte = (double?)req.maxPrice;
            filter.Add(range);
        }

        var sortField = string.IsNullOrWhiteSpace(req.sortBy) ? "createdAt" : req.sortBy!;
        var sortOrder = string.Equals(req.sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? SortOrder.Asc : SortOrder.Desc;

        var from = (Math.Max(1, req.page) - 1) * Math.Max(1, req.pageSize);
        var size = Math.Clamp(req.pageSize, 1, 100);

        var resp = await _client.SearchAsync<Product>(s => s
            .Index(_indexName)
            .From(from)
            .Size(size)
            .Query(q => q.Bool(b => b.Must(must.ToArray()).Filter(filter.ToArray())))
            .Sort(so => so.Field(new Field(sortField), sortOrder))
        , ct);

        if (!resp.IsValidResponse) throw new Exception(resp.DebugInformation);

        return new PagedResult<Product>(resp.Documents, resp.Total, req.page, req.pageSize);
    }

    // ---------- Bulk ----------
    public async Task<int> BulkIndexAsync(IEnumerable<Product> products, CancellationToken ct = default)
    {
        var ops = new BulkOperationsCollection();

        foreach (var p in products)
        {
            p.UpdatedAt = DateTime.UtcNow;

            var op = new BulkIndexOperation<Product>(p)
            {
                Id = p.Id
            };

            ops.Add(op);
        }

        var bulkReq = new BulkRequest(_indexName)
        {
            Operations = ops
        };

        var resp = await _client.BulkAsync(bulkReq, ct);

        var failed = resp.Items
            .Where(i => i.Status is < 200 or >= 300)
            .Select(i => $"{i.Operation} {_indexName}/{i.Id}: {i.Error?.Type} - {i.Error?.Reason}")
            .ToList();

        if (failed.Count > 0)
            throw new Exception("Bulk had item failures: " + string.Join(" | ", failed));

        return resp.Items.Count(i => i.Status is >= 200 and < 300);
    }

    public async Task UnblockIndexAsync(CancellationToken ct = default)
    {
        var resp = await _client.Indices.PutSettingsAsync(
            _indexName,
            ps => ps.Settings(s => s
                .Blocks(b => b.ReadOnlyAllowDelete(false))
            ),
            ct
        );

        if (!resp.IsValidResponse)
            throw new Exception("Failed to clear read-only block: " + resp.DebugInformation);
    }
}
