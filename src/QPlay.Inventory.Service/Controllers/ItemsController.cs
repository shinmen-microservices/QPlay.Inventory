using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPlay.Common.Repositories.Interfaces;
using QPlay.Inventory.Contracts;
using QPlay.Inventory.Service.Extensions;
using QPlay.Inventory.Service.Models.Dtos;
using QPlay.Inventory.Service.Models.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QPlay.Inventory.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private const string ADMIN = "Admin";
    private readonly IRepository<CatalogItem> catalogItemsRepository;
    private readonly IRepository<InventoryItem> inventoryItemsRepository;
    private readonly IPublishEndpoint publishEndpoint;

    public ItemsController(
        IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository,
        IPublishEndpoint publishEndpoint
    )
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest();

        string currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (Guid.Parse(currentUserId) != userId && !User.IsInRole(ADMIN))
        {
            return Forbid();
        }

        IReadOnlyCollection<InventoryItem> inventoryItems =
            await inventoryItemsRepository.GetAllAsync(item => item.UserId == userId);
        IEnumerable<Guid> inventoryItemCatalogItemIds = inventoryItems.Select(
            items => items.CatalogItemId
        );
        IReadOnlyCollection<CatalogItem> catalogItems = await catalogItemsRepository.GetAllAsync(
            item => inventoryItemCatalogItemIds.Contains(item.Id)
        );

        IEnumerable<InventoryItemDto> inventoryItemDtos = inventoryItems.Select(inventoryItem =>
        {
            CatalogItem catalogItem = catalogItems.Single(
                catalogItem => catalogItem.Id == inventoryItem.CatalogItemId
            );
            return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
        });

        return Ok(inventoryItemDtos);
    }

    [HttpPost]
    [Authorize(Roles = ADMIN)]
    public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
    {
        InventoryItem inventoryItem = await inventoryItemsRepository.GetAsync(
            item =>
                item.UserId == grantItemsDto.UserId
                && item.CatalogItemId == grantItemsDto.CatalogItemId
        );

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

        await publishEndpoint.Publish(
            new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            )
        );

        return Ok();
    }
}