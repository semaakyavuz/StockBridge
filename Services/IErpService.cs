namespace StockBridge.API.Services
{
    public interface IErpService
    {
        Task<bool> SyncProductAsync(int productId, string sku, string name, int quantity);
        Task<ErpSyncResult> GetSyncStatusAsync(string sku);
    }

    public class ErpSyncResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? LastSyncedAt { get; set; }
    }
}