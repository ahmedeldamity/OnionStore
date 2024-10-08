﻿using BlazorEcommerce.Application.Interfaces.Services;
using BlazorEcommerce.Domain.Entities.OrderEntities;
using BlazorEcommerce.Server.Extensions;
using BlazorEcommerce.Server.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BlazorEcommerce.Server.Controllers.V1;
public class DeliveryMethodController(IDeliveryMethodService deliveryMethodService) : BaseController
{
    [HttpGet]
    [Cached(600)]
    public async Task<ActionResult<IReadOnlyList<OrderDeliveryMethod>>> GetAllDeliveryMethods()
    {
        var result = await deliveryMethodService.GetAllDeliveryMethodsAsync();

        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    [Cached(600)]
    public async Task<ActionResult<OrderDeliveryMethod>> GetDeliveryMethodById(int id)
    {
        var result = await deliveryMethodService.GetDeliveryMethodByIdAsync(id);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    public async Task<ActionResult<OrderDeliveryMethod>> CreateDeliveryMethod(OrderDeliveryMethod deliveryMethod)
    {
        var result = await deliveryMethodService.CreateDeliveryMethodAsync(deliveryMethod);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderDeliveryMethod>> UpdateDeliveryMethod(int id, OrderDeliveryMethod deliveryMethod)
    {
        var result = await deliveryMethodService.UpdateDeliveryMethodAsync(id, deliveryMethod);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<OrderDeliveryMethod>> DeleteDeliveryMethod(int id)
    {
        var result = await deliveryMethodService.DeleteDeliveryMethodAsync(id);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

}