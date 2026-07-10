using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockBridge.API.Data;
using StockBridge.API.Services;

namespace StockBridge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIController : ControllerBase
    {
        private const int MovementHistoryDays = 30;

        private readonly IGroqService _groqService;
        private readonly AppDbContext _context;

        public AIController(IGroqService groqService, AppDbContext context)
        {
            _groqService = groqService;
            _context = context;
        }

        // Groq ile Türkçe ürün açıklaması üret
        [HttpPost("generate-description")]
        public async Task<IActionResult> GenerateDescription([FromBody] GenerateDescriptionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Sku) || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "SKU ve ürün adı gerekli." });

            try
            {
                var description = await _groqService.GenerateProductDescriptionAsync(req.Sku, req.Name);
                return Ok(new { description });
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = $"AI açıklama üretilemedi: {ex.Message}" });
            }
        }

        // Geçmiş stok hareketlerine göre her ürünün kaç günde tükeneceğini tahmin et
        [HttpGet("stock-forecast")]
        public async Task<IActionResult> GetStockForecast()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            var cutoff = DateTime.UtcNow.AddDays(-MovementHistoryDays);

            var recentOutMovements = await _context.StockMovements
                .Where(m => m.Type == "OUT" && m.CreatedAt >= cutoff)
                .ToListAsync();

            var movementsByProduct = recentOutMovements
                .GroupBy(m => m.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = products.Select(p =>
            {
                int? daysLeft = null;

                if (movementsByProduct.TryGetValue(p.Id, out var outMovements) && p.StockQuantity > 0)
                {
                    var totalOut = outMovements.Sum(m => m.Quantity);
                    var earliestDate = outMovements.Min(m => m.CreatedAt);
                    var daysSpan = Math.Max(1.0, (DateTime.UtcNow - earliestDate).TotalDays);
                    var dailyRate = totalOut / daysSpan;

                    if (dailyRate > 0)
                        daysLeft = (int)Math.Ceiling(p.StockQuantity / dailyRate);
                }

                return new StockForecastItem
                {
                    ProductId = p.Id,
                    DaysLeft = daysLeft
                };
            }).ToList();

            return Ok(result);
        }
    }

    public class GenerateDescriptionRequest
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class StockForecastItem
    {
        public int ProductId { get; set; }
        public int? DaysLeft { get; set; }
    }
}
