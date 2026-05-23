namespace StockBridge.API.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public string Type { get; set; } = string.Empty; // "IN" veya "OUT"
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}