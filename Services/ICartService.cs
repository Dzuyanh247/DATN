using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Services;

public interface ICartService
{
    Task<CartViewVm> GetCartAsync(int? userId);
    Task<CartViewVm> GetBuyNowCartAsync();
    Task<(bool Ok, string? Error)> AddToCartAsync(int? userId, int productId, int quantity = 1);
    Task<(bool Ok, string? Error)> SetBuyNowCartAsync(int? userId, int productId, int quantity = 1);
    Task<(bool Ok, string? Error)> SetBuyNowCartAsync(int? userId, IReadOnlyCollection<(int ProductId, int Quantity)> items);
    Task<(bool Ok, string? Error)> UpdateQuantityAsync(int? userId, int cartItemId, int quantity);
    Task RemoveItemAsync(int? userId, int cartItemId);
    Task ClearCartAsync(int? userId);
    Task ClearBuyNowCartAsync();
    Task MergeGuestCartAsync(int userId);
}
