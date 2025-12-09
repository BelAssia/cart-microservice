namespace CartAPI.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }

    public class Cart
    {
        public string UserId { get; set; } = string.Empty;
        public List<CartItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(x => x.Subtotal);
        public int TotalItems => Items.Sum(x => x.Quantity);
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}