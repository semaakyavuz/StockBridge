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
    public class SyncController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IErpService _erpService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(
            AppDbContext context,
            IErpService erpService,
            ILogger<SyncController> logger)
        {
            _context = context;
            _erpService = erpService;
            _logger = logger;
        }


        // Tek ürünü ERP ile senkronize et
        // Login olan kullanıcılar erişebilir
        [HttpPost("product/{id}")]
        public async Task<IActionResult> SyncProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();


            var success = await _erpService.SyncProductAsync(
                product.Id,
                product.Sku,
                product.Name,
                product.StockQuantity);


            if (success)
            {
                product.LastSyncedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"{product.Name} başarıyla ERP ile senkronize edildi.",
                    syncedAt = product.LastSyncedAt
                });
            }


            return StatusCode(503, new
            {
                message = "ERP servisi şu an yanıt vermiyor, tekrar deneyin."
            });
        }



        // Tüm ürünleri ERP ile senkronize et
        // Login olan kullanıcılar erişebilir
        [HttpPost("all")]
        public async Task<IActionResult> SyncAll()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();


            int success = 0;
            int failed = 0;


            foreach (var product in products)
            {
                var result = await _erpService.SyncProductAsync(
                    product.Id,
                    product.Sku,
                    product.Name,
                    product.StockQuantity);


                if (result)
                {
                    product.LastSyncedAt = DateTime.UtcNow;
                    success++;
                }
                else
                {
                    failed++;
                }
            }


            await _context.SaveChangesAsync();


            return Ok(new
            {
                message = "Senkronizasyon tamamlandı.",
                successCount = success,
                failedCount = failed,
                totalProducts = products.Count
            });
        }



        // Ürünün ERP durumunu sorgula
        // Login olan kullanıcılar erişebilir
        [HttpGet("status/{sku}")]
        public async Task<IActionResult> GetStatus(string sku)
        {
            var status = await _erpService.GetSyncStatusAsync(sku);

            return Ok(status);
        }
    }
}