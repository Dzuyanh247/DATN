using System.Security.Claims;
using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

public class CartController : Controller
{
    public class BundleCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public List<int> AccessoryProductIds { get; set; } = new();
    }
    private readonly ICartService _cartService;
    public CartController(ICartService cartService) => _cartService = cartService;

    public async Task<IActionResult> Index() => View(await _cartService.GetCartAsync(GetUserId()));

    [HttpPost]
    [ValidateAntiForgeryToken]
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
                message = "Đã thêm sản phẩm vào giỏ hàng!",
                cartCount = cart.Items.Sum(x => x.Quantity)
            });
        }

        if (!result.Ok)
        {
            TempData["ErrorMessage"] = "Không thể thêm sản phẩm vào giỏ hàng.";
        }
        else
        {
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer)) return Redirect(referer);
        return RedirectToAction("Index", "Products");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
    {
        var result = await _cartService.SetBuyNowCartAsync(GetUserId(), productId, quantity);
        if (!result.Ok)
        {
            TempData["ErrorMessage"] = result.Error ?? "Không thể mua sản phẩm này lúc này.";
            return RedirectToAction("Detail", "Products", new { id = productId });
        }

        return Redirect("/Checkout");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBundle([FromForm] BundleCartRequest request)
    {
        var items = new List<(int ProductId, int Quantity)> { (request.ProductId, Math.Max(1, request.Quantity)) };
        items.AddRange(request.AccessoryProductIds.Where(id => id > 0).Distinct().Select(id => (id, 1)));

        foreach (var item in items)
        {
            var result = await _cartService.AddToCartAsync(GetUserId(), item.ProductId, item.Quantity);
            if (!result.Ok)
            {
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = result.Error });
                TempData["ErrorMessage"] = result.Error ?? "Không thể thêm bộ sản phẩm vào giỏ hàng.";
                return Redirect(Request.Headers.Referer.ToString() ?? "/Products");
            }
        }

        if (IsAjaxRequest())
        {
            var cart = await _cartService.GetCartAsync(GetUserId());
            return Json(new { success = true, message = "Đã thêm PC và sản phẩm mua kèm vào giỏ hàng!", cartCount = cart.Items.Sum(x => x.Quantity) });
        }

        TempData["SuccessMessage"] = "Đã thêm PC và sản phẩm mua kèm vào giỏ hàng!";
        return Redirect(Request.Headers.Referer.ToString() ?? "/Cart");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyBundle(BundleCartRequest request)
    {
        var items = new List<(int ProductId, int Quantity)> { (request.ProductId, Math.Max(1, request.Quantity)) };
        items.AddRange(request.AccessoryProductIds.Where(id => id > 0).Distinct().Select(id => (id, 1)));
        var result = await _cartService.SetBuyNowCartAsync(GetUserId(), items);
        if (!result.Ok)
        {
            TempData["ErrorMessage"] = result.Error ?? "Không thể đặt bộ sản phẩm này lúc này.";
            return RedirectToAction("Detail", "Products", new { id = request.ProductId });
        }
        return Redirect("/Checkout");
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

    private int? GetUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue))
        {
            return null;
        }

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }
    private bool IsAjaxRequest()
        => Request.Headers.XRequestedWith == "XMLHttpRequest"
           || Request.Headers.Accept.Any(x => x.Contains("application/json", StringComparison.OrdinalIgnoreCase));
}
