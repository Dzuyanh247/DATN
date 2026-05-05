using System.Security.Claims;
using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly ICartService _cartService;

    public CartCountViewComponent(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int? userId = User.Identity?.IsAuthenticated == true
            ? int.Parse(UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!)
            : null;
        var cart = await _cartService.GetCartAsync(userId);
        var count = cart.Items.Sum(x => x.Quantity);
        return View(count);
    }
}
