using FinalTest.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalTest.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet("{id:int}/purchases")]
    public async Task<IActionResult> GetPurchases(int id)
    {
        try
        {
            var purchases = await customerService.GetPurchasesAsync(id);
            return Ok(purchases);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
