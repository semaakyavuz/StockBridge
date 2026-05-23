namespace StockBridge.API.Services
{
    public class MockIfsErpService : IErpService
    {
        private readonly ILogger<MockIfsErpService> _logger;
        private static readonly Dictionary<string, DateTime> _syncedItems = new();

        public MockIfsErpService(ILogger<MockIfsErpService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SyncProductAsync(int productId, string sku, string name, int quantity)
        {
            // Gerçek IFS API çağrısını simüle ediyoruz
            await Task.Delay(300); // IFS'in cevap süresini taklit et

            // %10 ihtimalle ERP'nin geçici hata verdiğini simüle et
            var random = new Random();
            if (random.Next(1, 11) == 1)
            {
                _logger.LogWarning("IFS ERP geçici hata verdi. SKU: {Sku}", sku);
                return false;
            }

            _syncedItems[sku] = DateTime.UtcNow;
            _logger.LogInformation("IFS ERP senkronize edildi. SKU: {Sku}, Adet: {Qty}", sku, quantity);
            return true;
        }

        public async Task<ErpSyncResult> GetSyncStatusAsync(string sku)
        {
            await Task.Delay(100);

            if (_syncedItems.TryGetValue(sku, out var syncedAt))
            {
                return new ErpSyncResult
                {
                    IsSuccess = true,
                    Message = "IFS'te kayıtlı",
                    LastSyncedAt = syncedAt
                };
            }

            return new ErpSyncResult
            {
                IsSuccess = false,
                Message = "IFS'te bulunamadı",
                LastSyncedAt = null
            };
        }
    }
}