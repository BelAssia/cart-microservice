#  Cart Microservice API

Microservice de gestion de panier avec Redis - ASP.NET Core 8

## API Endpoints

### Produits
- `GET /api/cart/products` - Liste des produits

### Panier
- `GET /api/cart/{userId}` - Voir le panier
- `POST /api/cart/{userId}/add` - Ajouter un produit
  ```json
  { "productId": 1, "quantity": 2 }
  ```
- `PUT /api/cart/{userId}/update` - Modifier quantité
  ```json
  { "productId": 1, "quantity": 5 }
  ```
- `DELETE /api/cart/{userId}/remove/{productId}` - Supprimer
- `DELETE /api/cart/{userId}/clear` - Vider le panier
- `POST /api/cart/{userId}/confirm` - Confirmer la commande

##  Test Local

```bash
docker-compose up -d
```

## Stack

- ASP.NET Core 8
- Redis
- Docker
