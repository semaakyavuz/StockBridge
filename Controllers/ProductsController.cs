using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockBridge.API.Data;
using StockBridge.API.Models;

namespace StockBridge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // tüm endpointler login gerektirir
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // Tüm ürünleri getir — user ve admin görebilir
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();
            return Ok(products);
        }

        // Tek ürün getir — user ve admin görebilir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // Yeni ürün ekle — SADECE admin
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // Ürün güncelle — SADECE admin
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, Product product)
        {
            if (id != product.Id) return BadRequest();

            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Sku = product.Sku;
            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.UnitPrice = product.UnitPrice;
            existing.StockQuantity = product.StockQuantity;
            existing.ReorderLevel = product.ReorderLevel;
            existing.IsActive = product.IsActive;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Ürün sil (soft delete) — SADECE admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Kritik stok — user ve admin görebilir
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var products = await _context.Products
                .Where(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)
                .ToListAsync();
            return Ok(products);
        }
    }
}