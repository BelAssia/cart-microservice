namespace CartAPI.Models
{
    // Classe representant un article dans le panier
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; } // Prix unitaire du produit
        public int Quantity { get; set; } // Qte de ce produit dans le panier
        public decimal Subtotal => Price * Quantity;// Sous-total pour cet article (prix * qte)
    }

    // Classe representant le panier d'un utilisateur
    public class Cart
    {
        // ID de l'utilisateur proprietaire du panier
        public string UserId { get; set; } = string.Empty;
        // Liste des articles dans le panier
        public List<CartItem> Items { get; set; } = new();
        // Total general du panier (somme de tous les sous-totaux)
        public decimal Total => Items.Sum(x => x.Subtotal);
        // Nombre total d'articles dans le panier
        public int TotalItems => Items.Sum(x => x.Quantity);
        // Date de derniere mise a jour du panier (UTC)
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}