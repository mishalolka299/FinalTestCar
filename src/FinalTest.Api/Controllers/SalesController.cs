using FluentValidation;
using FinalTest.Api.Contracts;
using FinalTest.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalTest.Api.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController(
    ISaleService saleService,
    IValidator<CreateSaleRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var sale = await saleService.CreateAsync(request);
            return CreatedAtAction(nameof(GetAll), new { }, sale);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        // Convert to UTC if provided
        var fromUtc = from.HasValue ? new DateTime(from.Value.Ticks, DateTimeKind.Utc) : (DateTime?)null;
        var toUtc = to.HasValue ? new DateTime(to.Value.Ticks, DateTimeKind.Utc) : (DateTime?)null;
        
        var sales = await saleService.GetAllAsync(fromUtc, toUtc);
        return Ok(sales);
    }
}
