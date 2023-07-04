using Microsoft.AspNetCore.Mvc;
using QPlay.Common.Repositories.Interfaces;
using QPlay.Inventory.Service.Clients;
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
    private readonly IRepository<InventoryItem> itemsRepository;
    private readonly CatalogClient catalogClient;

    public ItemsController(IRepository<InventoryItem> itemsRepository, CatalogClient catalogClient)
    {
        this.itemsRepository = itemsRepository;
        this.catalogClient = catalogClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        IReadOnlyCollection<CatalogItemDto> catalogItems = await catalogClient.GetCatalogItemsAsync();
        IReadOnlyCollection<InventoryItem> inventoryItemEntities = await itemsRepository.GetAllAsync(item => item.UserId == userId);

        IEnumerable<InventoryItemDto> inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
        {
            CatalogItemDto catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
            return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
        });

        return Ok(inventoryItemDtos);
    }

    [HttpPost]
    public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
    {
        InventoryItem inventoryItem = await itemsRepository.GetAsync(
            item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);

        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                CatalogItemId = grantItemsDto.CatalogItemId,
                UserId = grantItemsDto.UserId,
                Quantity = grantItemsDto.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };

            await itemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity += grantItemsDto.Quantity;
            await itemsRepository.UpdateAsync(inventoryItem);
        }

        return Ok();
    }
}
