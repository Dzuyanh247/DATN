using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BrandsApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<string>>> Get([FromQuery] string? componentType)
    {
        var query = _db.Products.AsNoTracking()
            .Where(product => product.ProductType == ProductKinds.Component
                && product.Brand != null
                && product.Brand != ""
                && product.Brand != "N/A");

        if (!string.IsNullOrWhiteSpace(componentType))
        {
            var normalizedType = componentType.Trim();
            query = query.Where(product => product.ComponentType == normalizedType);
        }

        var brands = await query
            .Select(product => product.Brand!.Trim())
            .Distinct()
            .OrderBy(brand => brand)
            .ToListAsync();

        return brands;
    }
}
