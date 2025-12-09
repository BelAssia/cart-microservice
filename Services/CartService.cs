using CartAPI.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace CartAPI.Services
{
    public class CartService
    {
        // Connexion Redis injectee via le constructeur
        private readonly IConnectionMultiplexer _redis;
        // Service local permettant de verifier les produits et le stock
        private readonly ProductService _productService;
        //  Logger pour enregistrer les informations importantes comme la confirmation de commande
        private readonly ILogger<CartService> _logger;

        //Injection des dependances via le constructeur
        public CartService(IConnectionMultiplexer redis, ProductService productService, ILogger<CartService> logger)
        {
            _redis = redis;
            _productService = productService;
            _logger = logger;
        }

        // Recupere la base Redis courante
        private IDatabase GetDb() => _redis.GetDatabase();
        
        // Generation  d'une cle unique dans Redis pour stocker le panier d’un utilisateur
        private string GetKey(string userId) => $"cart:{userId}";

        //Recupere le panier d’un utilisateur. Si le panier n'existe pas on retourne un panier vide.
        public async Task<Cart> GetCartAsync(string userId)
        {
            var db = GetDb();
            var data = await db.StringGetAsync(GetKey(userId));
            // Si aucune donnee on renvoie un panier vide
            if (data.IsNullOrEmpty)
                return new Cart { UserId = userId };
            // Deserialisation de JSON en Cart
            //Si le JSON est valide on retourne le panier
            //Si JSON est vide ou corrompu on retourne un panier neuf
            return JsonSerializer.Deserialize<Cart>(data!) ?? new Cart { UserId = userId };
        }

        // Ajout d’un produit au panier avec validation du stock, de la quantite, et mise a jour du panier
        public async Task<(bool success, string message)> AddToCartAsync(string userId, int productId, int quantity)
        {
            // Validation de la quantite
            if (quantity <= 0)
                return (false, "La quantite doit etre superieure a 0");

            // Verifier que le produit existe
            var product = _productService.GetProductById(productId);
            if (product == null)
                return (false, "Produit introuvable");

            // Recuperer le panier
            var cart = await GetCartAsync(userId);

            // Chercher si l'utilisateur a deja cet article dans son panier
            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == productId);

            // Calculer la quantite totale demandee
            int totalQuantity = quantity + (existingItem?.Quantity ?? 0);

            // Verification du stock disponible
            if (!_productService.IsStockAvailable(productId, totalQuantity))
                return (false, $"Stock insuffisant. Disponible: {product.Stock}");

            // Ajouter ou mettre a jour
            if (existingItem != null)
            {
                // Mise à jour de la quantite si l'article existe deja
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

            // Mise a jour de la date de modification
            cart.UpdatedAt = DateTime.UtcNow;

            // Enregistrement en base Redis
            await SaveCartAsync(cart);

            return (true, "Produit ajoute au panier");
        }

        //  Mise a jour de la quantita d'un item dans le panier
        public async Task<(bool success, string message)> UpdateQuantityAsync(string userId, int productId, int quantity)
        {
            // Si quantite = 0 on va supprimer l'article de panier
            if (quantity == 0)
                return await RemoveItemAsync(userId, productId);

            if (quantity < 0)
                return (false, "La quantite ne peut pas etre negative");

            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
                return (false, "Article non trouve dans le panier");

            // Verifier le stock
            if (!_productService.IsStockAvailable(productId, quantity))
            {
                var product = _productService.GetProductById(productId);
                return (false, $"Stock insuffisant. Disponible: {product?.Stock ?? 0}");
            }

            // Mise a jour de la quantite
            item.Quantity = quantity;
            cart.UpdatedAt = DateTime.UtcNow;
            await SaveCartAsync(cart);

            return (true, "Quantite mise a jour");
        }

        // Suppression d’un produit du panier
        public async Task<(bool success, string message)> RemoveItemAsync(string userId, int productId)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
                return (false, "Article non trouve");

            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await SaveCartAsync(cart);

            return (true, "Article supprime");
        }

        // Supprimer entierement le panier (cle Redis sera supprimee)
        public async Task<(bool success, string message)> ClearCartAsync(string userId)
        {
            var db = GetDb();
            await db.KeyDeleteAsync(GetKey(userId));
            return (true, "Panier vide");
        }

        // Confirme le panier, on verifie les stocks, genere un numero de commande, vide le panier
        public async Task<(bool success, string message, string? orderId)> ConfirmCartAsync(string userId)
        {
            var cart = await GetCartAsync(userId);

            // Verifier que le panier n'est pas vide
            if (!cart.Items.Any())
                return (false, "Le panier est vide", null);

            // Verifier le stock pour tous les articles
            foreach (var item in cart.Items)
            {
                if (!_productService.IsStockAvailable(item.ProductId, item.Quantity))
                {
                    var product = _productService.GetProductById(item.ProductId);
                    return (false, $"Stock insuffisant pour {item.ProductName}. Disponible: {product?.Stock ?? 0}", null);
                }
            }

            // Generer d'un ID unique pour la commande, par exemple : ORD-20251207-AB12F98C
            var orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            _logger.LogInformation("Commande creee: {OrderId} pour {UserId}, Total: {Total}€",
                orderId, userId, cart.Total);

            // Vider le panier apres confirmation
            await ClearCartAsync(userId);

            return (true, "Commande confirmee avec succes", orderId);
        }

        // Enregistre le panier dans Redis, en JSON, avec expiration de 14 jours
        private async Task SaveCartAsync(Cart cart)
        {
            var db = GetDb();
            var json = JsonSerializer.Serialize(cart);
            // StringSetAsync(key, value, expire)
            await db.StringSetAsync(GetKey(cart.UserId), json, TimeSpan.FromDays(14));
        }
    }
}