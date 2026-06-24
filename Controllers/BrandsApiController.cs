using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
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
        var normalizedType = NormalizeComponentType(componentType);
        var typeAliases = ComponentTypes.GetAliases(normalizedType);
        var productBrands = await _db.Products.AsNoTracking()
            .Where(product => product.ProductType == ProductKinds.Component
                && typeAliases.Contains(product.ComponentType)
                && product.Brand != null
                && product.Brand != ""
                && product.Brand != "N/A")
            .Select(product => product.Brand!.Trim())
            .ToListAsync();

        var catalogBrands = await _db.ComponentBrands.AsNoTracking()
            .Where(brand => typeAliases.Contains(brand.ComponentType))
            .Select(brand => brand.Name.Trim())
            .ToListAsync();

        return productBrands.Concat(catalogBrands)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(brand => brand)
            .ToList();
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateComponentBrandRequest? request)
    {
        var name = request?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Vui lòng nhập tên thương hiệu." });

        var componentType = NormalizeComponentType(request?.ComponentType);
        var typeAliases = ComponentTypes.GetAliases(componentType);
        var existsInCatalog = await _db.ComponentBrands.AnyAsync(brand => typeAliases.Contains(brand.ComponentType) && brand.Name.ToLower() == name.ToLower());
        var existsInProducts = await _db.Products.AsNoTracking().AnyAsync(product => product.ProductType == ProductKinds.Component
            && typeAliases.Contains(product.ComponentType)
            && product.Brand != null
            && product.Brand.ToLower() == name.ToLower());

        if (existsInCatalog || existsInProducts)
            return Conflict(new { message = "Thương hiệu này đã tồn tại." });

        var brand = new ComponentBrand { Name = name, ComponentType = componentType };
        _db.ComponentBrands.Add(brand);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Thương hiệu này đã tồn tại." });
        }

        return Ok(new { name = brand.Name, componentType = brand.ComponentType });
    }

    private static string NormalizeComponentType(string? componentType)
        => ComponentTypes.Normalize(componentType);
}

public sealed class CreateComponentBrandRequest
{
    public string? Name { get; set; }
    public string? ComponentType { get; set; }
}
