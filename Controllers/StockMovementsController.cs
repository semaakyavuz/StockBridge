using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockBridge.API.Data;
using StockBridge.API.Models;

namespace StockBridge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockMovementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockMovementsController(AppDbContext context)
        {
            _context = context;
        }

        // Ürünün tüm hareketleri
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var movements = await _context.StockMovements
                .Where(m => m.ProductId == productId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new {
                    m.Id,
                    m.ProductId,
                    m.Quantity,
                    m.Type,
                    m.Reason,
                    m.CreatedAt
                })
                .ToListAsync();

            return Ok(movements);
        }

        // Stok hareketi ekle
        [HttpPost]
        public async Task<IActionResult> AddMovement([FromBody] StockMovementRequest req)
        {
            var product = await _context.Products.FindAsync(req.ProductId);
            if (product == null) return NotFound();

            if (req.Type == "OUT" && product.StockQuantity < req.Quantity)
                return BadRequest(new { message = "Yetersiz stok!" });

            if (req.Type == "IN")
                product.StockQuantity += req.Quantity;
            else
                product.StockQuantity -= req.Quantity;

            var movement = new StockMovement
            {
                ProductId = req.ProductId,
                Quantity = req.Quantity,
                Type = req.Type,
                Reason = req.Reason
            };

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Stok hareketi kaydedildi. Yeni stok: {product.StockQuantity}",
                newStock = product.StockQuantity
            });
        }

        // Son hareketler (genel)
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent()
        {
            var movements = await _context.StockMovements
                .Include(m => m.Product)
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .Select(m => new {
                    m.Id,
                    m.Quantity,
                    m.Type,
                    m.Reason,
                    m.CreatedAt,
                    ProductName = m.Product.Name,
                    ProductSku = m.Product.Sku
                })
                .ToListAsync();

            return Ok(movements);
        }
    }

    public class StockMovementRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}