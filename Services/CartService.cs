using CartAPI.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace CartAPI.Services
{
    public class CartService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ProductService _productService;
        private readonly ILogger<CartService> _logger;

        public CartService(IConnectionMultiplexer redis, ProductService productService, ILogger<CartService> logger)
        {
            _redis = redis;
            _productService = productService;
            _logger = logger;
        }

        private IDatabase GetDb() => _redis.GetDatabase();
        private string GetKey(string userId) => $"cart:{userId}";

        public async Task<Cart> GetCartAsync(string userId)
        {
            var db = GetDb();
            var data = await db.StringGetAsync(GetKey(userId));

            if (data.IsNullOrEmpty)
                return new Cart { UserId = userId };

            return JsonSerializer.Deserialize<Cart>(data!) ?? new Cart { UserId = userId };
        }

        public async Task<(bool success, string message)> AddToCartAsync(string userId, int productId, int quantity)
        {
            // Validation de la quantité
            if (quantity <= 0)
                return (false, "La quantité doit être supérieure à 0");

            // Vérifier que le produit existe
            var product = _productService.GetProductById(productId);
            if (product == null)
                return (false, "Produit introuvable");

            // Récupérer le panier
            var cart = await GetCartAsync(userId);
            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == productId);

            // Calculer la quantité totale demandée
            int totalQuantity = quantity + (existingItem?.Quantity ?? 0);

            // Vérifier le stock disponible
            if (!_productService.IsStockAvailable(productId, totalQuantity))
                return (false, $"Stock insuffisant. Disponible: {product.Stock}");

            // Ajouter ou mettre à jour
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await SaveCartAsync(cart);

            return (true, "Produit ajouté au panier");
        }

        public async Task<(bool success, string message)> UpdateQuantityAsync(string userId, int productId, int quantity)
        {
            // Si quantité = 0, supprimer
            if (quantity == 0)
                return await RemoveItemAsync(userId, productId);

            if (quantity < 0)
                return (false, "La quantité ne peut pas être négative");

            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
                return (false, "Article non trouvé dans le panier");

            // Vérifier le stock
            if (!_productService.IsStockAvailable(productId, quantity))
            {
                var product = _productService.GetProductById(productId);
                return (false, $"Stock insuffisant. Disponible: {product?.Stock ?? 0}");
            }

            item.Quantity = quantity;
            cart.UpdatedAt = DateTime.UtcNow;
            await SaveCartAsync(cart);

            return (true, "Quantité mise à jour");
        }

        public async Task<(bool success, string message)> RemoveItemAsync(string userId, int productId)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
                return (false, "Article non trouvé");

            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await SaveCartAsync(cart);

            return (true, "Article supprimé");
        }

        public async Task<(bool success, string message)> ClearCartAsync(string userId)
        {
            var db = GetDb();
            await db.KeyDeleteAsync(GetKey(userId));
            return (true, "Panier vidé");
        }

        public async Task<(bool success, string message, string? orderId)> ConfirmCartAsync(string userId)
        {
            var cart = await GetCartAsync(userId);

            // Vérifier que le panier n'est pas vide
            if (!cart.Items.Any())
                return (false, "Le panier est vide", null);

            // Vérifier le stock pour tous les articles
            foreach (var item in cart.Items)
            {
                if (!_productService.IsStockAvailable(item.ProductId, item.Quantity))
                {
                    var product = _productService.GetProductById(item.ProductId);
                    return (false, $"Stock insuffisant pour {item.ProductName}. Disponible: {product?.Stock ?? 0}", null);
                }
            }

            // Générer un ID de commande
            var orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            _logger.LogInformation("Commande créée: {OrderId} pour {UserId}, Total: {Total}€",
                orderId, userId, cart.Total);

            // Vider le panier après confirmation
            await ClearCartAsync(userId);

            return (true, "Commande confirmée avec succès", orderId);
        }

        private async Task SaveCartAsync(Cart cart)
        {
            var db = GetDb();
            var json = JsonSerializer.Serialize(cart);
            await db.StringSetAsync(GetKey(cart.UserId), json, TimeSpan.FromDays(7));
        }
    }
}