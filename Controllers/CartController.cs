using CartAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CartAPI.Controllers
{
    [ApiController]
    // on definit la route de base pour ce controleur : "api/cart"
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService; // Service pour gérer le panier
        private readonly ProductService _productService; // Service pour gérer les produits

        // Constructeur avec injection de dépendances
        public CartController(CartService cartService, ProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        // GET api/cart/products
        // Récupère tous les produits disponibles
        [HttpGet("products")]
        public IActionResult GetProducts()
        {
            var products = _productService.GetAllProducts();
            return Ok(products); // Retourne la liste des produits avec un status 200
        }

        // GET api/cart/{userId}
        // Recupere le panier d'un utilisateur specifique
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(string userId)
        {
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart); // Retourne le panier avec un status 200
        }

        // POST api/cart/{userId}/add
        // Ajoute un produit au panier de l'utilisateur
        [HttpPost("{userId}/add")]
        public async Task<IActionResult> AddToCart(string userId, [FromBody] AddToCartRequest request)
        {
            // Ajoute le produit au panier et recupere le resultat
            var (success, message) = await _cartService.AddToCartAsync(userId, request.ProductId, request.Quantity);

            // Retourne une erreur si l'ajout echoue
            if (!success)
                return BadRequest(new { message }); 

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(new { message, cart }); // Retourne un message et le panier mis a jour
        }

        // PUT api/cart/{userId}/update
        // Met a jour la quantite d'un produit dans le panier
        [HttpPut("{userId}/update")]
        public async Task<IActionResult> UpdateQuantity(string userId, [FromBody] UpdateQuantityRequest request)
        {
            var (success, message) = await _cartService.UpdateQuantityAsync(userId, request.ProductId, request.Quantity);

            // Retourne une erreur si la mise a jour echoue
            if (!success)
                return BadRequest(new { message });

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(new { message, cart });// Retourne un message et le panier mis a jour
        }

        // DELETE api/cart/{userId}/remove/{productId}
        // Supprime un produit specifique du panier
        [HttpDelete("{userId}/remove/{productId}")]
        public async Task<IActionResult> RemoveItem(string userId, int productId)
        {
            var (success, message) = await _cartService.RemoveItemAsync(userId, productId);

            // Retourne une erreur si la suppression echoue
            if (!success)
                return BadRequest(new { message });

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(new { message, cart });// Retourne un message et le panier mis a jour
        }

        // DELETE api/cart/{userId}/clear
        // Vide completement le panier de l'utilisateur
        [HttpDelete("{userId}/clear")]
        public async Task<IActionResult> ClearCart(string userId)
        {
            var (success, message) = await _cartService.ClearCartAsync(userId);
            return Ok(new { message }); // Retourne un message confirmant que le panier est vide
        }

        // POST api/cart/{userId}/confirm
        // Confirme le panier et genere une commande
        [HttpPost("{userId}/confirm")]
        public async Task<IActionResult> ConfirmCart(string userId)
        {
            var (success, message, orderId) = await _cartService.ConfirmCartAsync(userId);

            // Retourne une erreur si la confirmation echoue
            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, orderId });// Retourne le message et l'identifiant de la commande
        }
    }

    //=> Classes pour les requêtes HTTP
    // Represente les donnees necessaires pour ajouter un produit au panier
    public class AddToCartRequest
    {
        public int ProductId { get; set; } //id du produit a ajouter
        public int Quantity { get; set; } = 1;
    }

    // Represente les donnees necessaires pour mettre a jour la quantite d'un produit
    public class UpdateQuantityRequest
    {
        public int ProductId { get; set; } //id du produit a modifier
        public int Quantity { get; set; } //nouvelle quantite
    }
}