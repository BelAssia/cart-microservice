using CartAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CartAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;
        private readonly ProductService _productService;

        public CartController(CartService cartService, ProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        [HttpGet("products")]
        public IActionResult GetProducts()
        {
            var products = _productService.GetAllProducts();
            return Ok(products);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(string userId)
        {
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart);
        }

        [HttpPost("{userId}/add")]
        public async Task<IActionResult> AddToCart(string userId, [FromBody] AddToCartRequest request)
        {
            var (success, message) = await _cartService.AddToCartAsync(userId, request.ProductId, request.Quantity);

            if (!success)
                return BadRequest(new { message });

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(new { message, cart });
        }

        [HttpPut("{userId}/update")]
        public async Task<IActionResult> UpdateQuantity(string userId, [FromBody] UpdateQuantityRequest request)
        {
            var (success, message) = await _cartService.UpdateQuantityAsync(userId, request.ProductId, request.Quantity);

            if (!success)
                return BadRequest(new { message });

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(new { message, cart });
        }

        [HttpDelete("{userId}/remove/{productId}")]
        public async Task<IActionResult> RemoveItem(string userId, int productId)
        {
            var (success, message) = await _cartService.RemoveItemAsync(userId, productId);

            if (!success)
                return BadRequest(new { message });

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(new { message, cart });
        }

        [HttpDelete("{userId}/clear")]
        public async Task<IActionResult> ClearCart(string userId)
        {
            var (success, message) = await _cartService.ClearCartAsync(userId);
            return Ok(new { message });
        }

        [HttpPost("{userId}/confirm")]
        public async Task<IActionResult> ConfirmCart(string userId)
        {
            var (success, message, orderId) = await _cartService.ConfirmCartAsync(userId);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, orderId });
        }
    }

    // Classes pour les requêtes (minimal, pas de DTO séparé)
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}