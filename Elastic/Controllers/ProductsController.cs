using ElasticTest.Models;
using ElasticTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElasticTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(ElasticService svc) : ControllerBase
    {
        private readonly ElasticService _svc = svc;





        [HttpPost("seed")]
        public async Task<IResult> Seed(CancellationToken ct)
        {
            var items = Enumerable.Range(1, 10).Select(i => new Product
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = $"Sample Product {i}",
                Description = "A demo product indexed into Elasticsearch",
                Category = i % 2 == 0 ? "Electronics" : "Clothing",
                Price = i % 2 == 0 ? 199.99m + i : 49.99m + i,
                Stock = 100 - i
            });
            await _svc.UnblockIndexAsync(ct);

            var count = await _svc.BulkIndexAsync(items, ct);
            return Results.Ok(new { indexed = count });
        }

        [HttpPost]
        public async Task<IResult> Create(Product product, CancellationToken ct)
        {
            await _svc.UnblockIndexAsync(ct);
            await _svc.IndexAsync(product, ct);
            return Results.Created($"/api/products/{product.Id}", product);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> Get(string id, CancellationToken ct)
        {
            var p = await _svc.GetAsync(id, ct);
            return p is null ? NotFound() : Ok(p);
        }

        public record UpdateDto(string? Name, string? Description, string? Category, decimal? Price, int? Stock);

        [HttpPatch("{id}")]
        public async Task<IResult> Update(string id, UpdateDto dto, CancellationToken ct)
        {
            var ok = await _svc.UpdatePartialAsync(id, p =>
            {
                if (dto.Name is not null) p.Name = dto.Name;
                if (dto.Description is not null) p.Description = dto.Description;
                if (dto.Category is not null) p.Category = dto.Category;
                if (dto.Price is not null) p.Price = dto.Price.Value;
                if (dto.Stock is not null) p.Stock = dto.Stock.Value;
            }, ct);

            return ok ? Results.Ok() : Results.NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IResult> Delete(string id, CancellationToken ct)
        {
            var ok = await _svc.DeleteAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }

        [HttpGet("search")]
        public async Task<IResult> Search([FromQuery] ElasticService.ProductSearchRequest req, CancellationToken ct)
        {
            var res = await _svc.SearchAsync(req, ct);
            return Results.Ok(res);
        }
    }
}