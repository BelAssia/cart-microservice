using CartAPI.Models;

namespace CartAPI.Services
{
    // Service pour gerer les produits de l'application
    public class ProductService
    {
        // Liste en memoire des produits disponibles, on initialise quelques produits
        private readonly List<Product> _products = new()
        {
            new Product { Id = 1, Name = "Laptop Dell", Price = 999.99m, Stock = 10 },
            new Product { Id = 2, Name = "iPhone 15", Price = 1099.99m, Stock = 15},
            new Product { Id = 3, Name = "Sony Headphones", Price = 299.99m, Stock = 20 },
            new Product { Id = 4, Name = "Samsung TV", Price = 799.99m, Stock = 8 },
            new Product { Id = 5, Name = "Apple Watch", Price = 399.99m, Stock = 12 }
        };

        // Retourne tous les produits de la liste
        public List<Product> GetAllProducts()
        {
            return _products;
        }

        // Retourne un produit specifique par son Id
        // Si le produit n'existe pas on retourne null
        public Product? GetProductById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        // Verifie si une certaine quantite d'un produit est disponible en stock
        public bool IsStockAvailable(int productId, int quantity)
        {
            var product = GetProductById(productId);
            return product != null && product.Stock >= quantity;
        }
    }
}