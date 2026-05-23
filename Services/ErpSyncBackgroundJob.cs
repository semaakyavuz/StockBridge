using Microsoft.EntityFrameworkCore;
using StockBridge.API.Data;

namespace StockBridge.API.Services
{
    public class ErpSyncBackgroundJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ErpSyncBackgroundJob> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

        public ErpSyncBackgroundJob(IServiceScopeFactory scopeFactory, ILogger<ErpSyncBackgroundJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ERP Sync Background Job başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncAllProductsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync sırasında hata oluştu.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task SyncAllProductsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var erpService = scope.ServiceProvider.GetRequiredService<IErpService>();

            var products = await context.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            int success = 0, failed = 0;

            foreach (var product in products)
            {
                var result = await erpService.SyncProductAsync(
                    product.Id, product.Sku, product.Name, product.StockQuantity);

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

            await context.SaveChangesAsync();
            _logger.LogInformation("Otomatik sync tamamlandı. Başarılı: {S}, Başarısız: {F}", success, failed);
        }
    }
}