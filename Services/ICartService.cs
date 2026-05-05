using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Services;

public interface ICartService
{
    Task<CartViewVm> GetCartAsync(int? userId);
    Task<(bool Ok, string? Error)> AddToCartAsync(int? userId, int productId, int quantity = 1);
    Task<(bool Ok, string? Error)> UpdateQuantityAsync(int? userId, int cartItemId, int quantity);
    Task RemoveItemAsync(int? userId, int cartItemId);
    Task ClearCartAsync(int? userId);
    Task MergeGuestCartAsync(int userId);
}
