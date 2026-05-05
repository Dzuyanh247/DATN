using System.Security.Claims;
using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    public CartController(ICartService cartService) => _cartService = cartService;

    public async Task<IActionResult> Index() => View(await _cartService.GetCartAsync(GetUserId()));

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var result = await _cartService.AddToCartAsync(GetUserId(), productId, quantity);
        if (IsAjaxRequest())
        {
            if (!result.Ok)
            {
                return BadRequest(new { success = false, message = result.Error ?? "Không thể thêm vào giỏ hàng." });
            }

            var cart = await _cartService.GetCartAsync(GetUserId());
            return Json(new
            {
                success = true,
                message = "Đã thêm sản phẩm vào giỏ hàng",
                cartCount = cart.Items.Sum(x => x.Quantity)
            });
        }

        if (!result.Ok)
        {
            TempData["CartError"] = result.Error;
        }
        else
        {
            TempData["CartSuccess"] = "Đã thêm sản phẩm vào giỏ hàng";
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer)) return Redirect(referer);
        return RedirectToAction("Index", "Products");
    }

    [HttpPost]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var result = await _cartService.UpdateQuantityAsync(GetUserId(), cartItemId, quantity);
        if (!result.Ok) TempData["CartError"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        await _cartService.RemoveItemAsync(GetUserId(), cartItemId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearCartAsync(GetUserId());
        return RedirectToAction(nameof(Index));
    }

    private int? GetUserId() => User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : null;
    private bool IsAjaxRequest()
        => Request.Headers.XRequestedWith == "XMLHttpRequest"
           || Request.Headers.Accept.Any(x => x.Contains("application/json", StringComparison.OrdinalIgnoreCase));
}
