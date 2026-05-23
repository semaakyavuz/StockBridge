using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockBridge.API.Data;
using StockBridge.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockBridge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // Tüm ürünleri getir
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();
            return Ok(products);
        }

        // Tek ürün getir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // Yeni ürün ekle
        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // Ürün güncelle
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product product)
        {
            if (id != product.Id) return BadRequest();
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Ürün sil (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Kritik stok seviyesindeki ürünler
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