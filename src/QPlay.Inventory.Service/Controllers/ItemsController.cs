using MassTransit;
using Microsoft.AspNetCore.Mvc;
using QPlay.Common.Repositories.Interfaces;
using QPlay.Inventory.Service.Extensions;
using QPlay.Inventory.Service.Models.Dtos;
using QPlay.Inventory.Service.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QPlay.Inventory.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly IRepository<InventoryItem> inventoryItemsRepository;
    private readonly IRepository<CatalogItem> catalogItemsRepository;

    public ItemsController(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty) return BadRequest();

        IReadOnlyCollection<InventoryItem> inventoryItems = await inventoryItemsRepository
            .GetAllAsync(item => item.UserId == userId);
        IEnumerable<Guid> inventoryItemCatalogItemIds = inventoryItems.Select(items => items.CatalogItemId);
        IReadOnlyCollection<CatalogItem> catalogItems = await catalogItemsRepository
            .GetAllAsync(item => inventoryItemCatalogItemIds.Contains(item.Id));

        IEnumerable<InventoryItemDto> inventoryItemDtos = inventoryItems.Select(inventoryItem =>
        {
            CatalogItem catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
            return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
        });

        return Ok(inventoryItemDtos);
    }

    [HttpPost]
    public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
    {
        InventoryItem inventoryItem = await inventoryItemsRepository
            .GetAsync(item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);

        if (inventoryItem == null)
        {
            inventoryItem = new()
            {
                CatalogItemId = grantItemsDto.CatalogItemId,
                UserId = grantItemsDto.UserId,
                Quantity = grantItemsDto.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };

            await inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity += grantItemsDto.Quantity;
            await inventoryItemsRepository.UpdateAsync(inventoryItem);
        }

        return Ok();
    }
}
