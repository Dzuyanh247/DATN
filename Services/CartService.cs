using System.Text.Json;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public class CartService : ICartService
{
    private const string SessionCartKey = "guest_cart";
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    public CartService(ApplicationDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public async Task<CartViewVm> GetCartAsync(int? userId)
    {
        return userId.HasValue ? await BuildDbCartAsync(userId.Value) : await BuildSessionCartAsync();
    }

    public async Task<(bool Ok, string? Error)> AddToCartAsync(int? userId, int productId, int quantity = 1)
    {
        quantity = Math.Max(quantity, 1);
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);
        if (p == null) return (false, "Sản phẩm không tồn tại.");
        if (p.StockQuantity < quantity) return (false, "Sản phẩm không đủ tồn kho.");
        if (userId.HasValue)
        {
            var cart = await GetOrCreateCartAsync(userId.Value);
            var existing = await _db.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
            if (existing == null) _db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = productId, Quantity = quantity });
            else existing.Quantity = Math.Min(existing.Quantity + quantity, p.StockQuantity);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        var items = GetSessionItems();
        var key = -productId;
        var line = items.FirstOrDefault(x => x.CartItemId == key);
        if (line == null) items.Add(new CartLineVm { CartItemId = key, ProductId = productId, Quantity = Math.Min(quantity, p.StockQuantity) });
        else line.Quantity = Math.Min(line.Quantity + quantity, p.StockQuantity);
        SaveSessionItems(items);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetBuyNowCartAsync(int? userId, int productId, int quantity = 1)
    {
        quantity = Math.Max(quantity, 1);
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);
        if (product == null) return (false, "Sản phẩm không tồn tại.");
        if (product.StockQuantity < quantity) return (false, "Sản phẩm không đủ tồn kho.");

        if (userId.HasValue)
        {
            var cart = await GetOrCreateCartAsync(userId.Value);
            var existingItems = await _db.CartItems.Where(x => x.CartId == cart.Id).ToListAsync();
            var buyNowItem = existingItems.FirstOrDefault(x => x.ProductId == productId);
            _db.CartItems.RemoveRange(existingItems.Where(x => x.ProductId != productId));
            if (buyNowItem == null)
            {
                _db.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                });
            }
            else
            {
                buyNowItem.Quantity = quantity;
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }

        SaveSessionItems(new List<CartLineVm>
        {
            new()
            {
                CartItemId = -productId,
                ProductId = productId,
                Quantity = quantity
            }
        });
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateQuantityAsync(int? userId, int cartItemId, int quantity)
    {
        quantity = Math.Max(quantity, 1);
        if (userId.HasValue)
        {
            var cart = await GetOrCreateCartAsync(userId.Value);
            var item = await _db.CartItems.Include(x => x.Product).FirstOrDefaultAsync(i => i.Id == cartItemId && i.CartId == cart.Id);
            if (item == null) return (false, "Không tìm thấy sản phẩm trong giỏ.");
            if (item.Product != null && quantity > item.Product.StockQuantity) return (false, "Vượt quá tồn kho.");
            item.Quantity = quantity;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        var items = GetSessionItems();
        var line = items.FirstOrDefault(x => x.CartItemId == cartItemId);
        if (line == null) return (false, "Không tìm thấy sản phẩm trong giỏ.");
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == line.ProductId);
        if (p == null) return (false, "Sản phẩm không tồn tại.");
        if (quantity > p.StockQuantity) return (false, "Vượt quá tồn kho.");
        line.Quantity = quantity;
        SaveSessionItems(items);
        return (true, null);
    }

    public async Task RemoveItemAsync(int? userId, int cartItemId)
    {
        if (userId.HasValue)
        {
            var cart = await GetOrCreateCartAsync(userId.Value);
            var item = await _db.CartItems.FirstOrDefaultAsync(i => i.Id == cartItemId && i.CartId == cart.Id);
            if (item != null) { _db.CartItems.Remove(item); await _db.SaveChangesAsync(); }
            return;
        }
        var items = GetSessionItems().Where(x => x.CartItemId != cartItemId).ToList(); SaveSessionItems(items);
    }

    public async Task ClearCartAsync(int? userId)
    {
        if (userId.HasValue)
        {
            var cart = await GetOrCreateCartAsync(userId.Value);
            var items = await _db.CartItems.Where(x => x.CartId == cart.Id).ToListAsync();
            _db.CartItems.RemoveRange(items); await _db.SaveChangesAsync(); return;
        }
        SaveSessionItems(new());
    }

    public async Task MergeGuestCartAsync(int userId)
    {
        var guest = GetSessionItems();
        foreach (var i in guest) await AddToCartAsync(userId, i.ProductId, i.Quantity);
        SaveSessionItems(new());
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null) return cart;
        cart = new Cart { UserId = userId }; _db.Carts.Add(cart); await _db.SaveChangesAsync(); return cart;
    }

    private List<CartLineVm> GetSessionItems()
    {
        var s = _http.HttpContext?.Session.GetString(SessionCartKey);
        return string.IsNullOrWhiteSpace(s) ? new() : JsonSerializer.Deserialize<List<CartLineVm>>(s) ?? new();
    }
    private void SaveSessionItems(List<CartLineVm> items) => _http.HttpContext?.Session.SetString(SessionCartKey, JsonSerializer.Serialize(items));

    private async Task<CartViewVm> BuildDbCartAsync(int userId)
    {
        var cart = await _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(c => c.UserId == userId);
        var vm = new CartViewVm();
        if (cart == null) return vm;
        vm.Items = cart.Items.Where(i => i.Product != null).Select(i => new CartLineVm
        {
            CartItemId = i.Id, ProductId = i.ProductId, ProductName = i.Product!.Name, ProductImage = i.Product.ThumbnailImage,
            Warranty = i.Product.WarrantyDuration, UnitPrice = i.Product.DiscountPrice ?? i.Product.SalePrice ?? i.Product.Price,
            Quantity = i.Quantity, StockQuantity = i.Product.StockQuantity
        }).ToList();
        return vm;
    }

    private async Task<CartViewVm> BuildSessionCartAsync()
    {
        var raw = GetSessionItems();
        var ids = raw.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var vm = new CartViewVm();
        foreach (var i in raw)
        {
            if (!products.TryGetValue(i.ProductId, out var p)) continue;
            vm.Items.Add(new CartLineVm
            {
                CartItemId = i.CartItemId, ProductId = i.ProductId, ProductName = p.Name, ProductImage = p.ThumbnailImage,
                Warranty = p.WarrantyDuration, UnitPrice = p.DiscountPrice ?? p.SalePrice ?? p.Price,
                Quantity = Math.Min(Math.Max(i.Quantity, 1), p.StockQuantity), StockQuantity = p.StockQuantity
            });
        }
        SaveSessionItems(vm.Items.Select(x => new CartLineVm { CartItemId = x.CartItemId, ProductId = x.ProductId, Quantity = x.Quantity }).ToList());
        return vm;
    }
}
