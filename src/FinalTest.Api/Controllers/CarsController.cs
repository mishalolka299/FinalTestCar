using FluentValidation;
using FinalTest.Api.Contracts;
using FinalTest.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalTest.Api.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController(
    ICarService carService,
    IValidator<CreateCarRequest> createValidator,
    IValidator<UpdateCarRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? make,
        [FromQuery] int? yearFrom,
        [FromQuery] int? yearTo,
        [FromQuery] decimal? priceFrom,
        [FromQuery] decimal? priceTo,
        [FromQuery] string? fuelType,
        [FromQuery] string? status)
    {
        var cars = await carService.GetAllAsync(make, yearFrom, yearTo, priceFrom, priceTo, fuelType, status);
        return Ok(cars);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var car = await carService.GetByIdAsync(id);
            return Ok(car);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCarRequest request)
    {
        var validation = await createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var car = await carService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = car.Id }, car);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCarRequest request)
    {
        var validation = await updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var car = await carService.UpdateAsync(id, request);
            return Ok(car);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("{id:int}/reserve")]
    public async Task<IActionResult> Reserve(int id)
    {
        try
        {
            var car = await carService.ReserveAsync(id);
            return Ok(car);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
