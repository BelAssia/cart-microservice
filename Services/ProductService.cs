using CartAPI.Models;

namespace CartAPI.Services
{
    public class ProductService
    {
        private readonly List<Product> _products = new()
        {
            new Product { Id = 1, Name = "Laptop Dell", Price = 999.99m, Stock = 10, ImageUrl = "/img/laptop.jpg" },
            new Product { Id = 2, Name = "iPhone 15", Price = 1099.99m, Stock = 15, ImageUrl = "/img/iphone.jpg" },
            new Product { Id = 3, Name = "Sony Headphones", Price = 299.99m, Stock = 20, ImageUrl = "/img/headphones.jpg" },
            new Product { Id = 4, Name = "Samsung TV", Price = 799.99m, Stock = 8, ImageUrl = "/img/tv.jpg" },
            new Product { Id = 5, Name = "Apple Watch", Price = 399.99m, Stock = 12, ImageUrl = "/img/watch.jpg" }
        };

        public List<Product> GetAllProducts()
        {
            return _products;
        }

        public Product? GetProductById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public bool IsStockAvailable(int productId, int quantity)
        {
            var product = GetProductById(productId);
            return product != null && product.Stock >= quantity;
        }
    }
}