using MassTransit;
using QPlay.Common.Repositories.Interfaces;
using QPlay.Inventory.Contracts;
using QPlay.Inventory.Service.Exceptions;
using QPlay.Inventory.Service.Models.Entities;
using System;
using System.Threading.Tasks;

namespace QPlay.Inventory.Service.Consumers;

public class GrantItemsConsumer : IConsumer<GrantItems>
{
    private readonly IRepository<CatalogItem> catalogItemsRepository;
    private readonly IRepository<InventoryItem> inventoryItemsRepository;

    public GrantItemsConsumer(
        IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository
    )
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
    }

    public async Task Consume(ConsumeContext<GrantItems> context)
    {
        GrantItems message = context.Message;

        CatalogItem catalogItem =
            await catalogItemsRepository.GetAsync(message.CatalogItemId)
            ?? throw new UnknownItemException(message.CatalogItemId);

        InventoryItem inventoryItem = await inventoryItemsRepository.GetAsync(
            item => item.UserId == message.UserId && item.CatalogItemId == catalogItem.Id
        );

        if (inventoryItem == null)
        {
            inventoryItem = new()
            {
                CatalogItemId = message.CatalogItemId,
                UserId = message.UserId,
                Quantity = message.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };

            inventoryItem.MessageIds.Add(context.MessageId.Value);

            await inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                return;
            }

            inventoryItem.Quantity += message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await inventoryItemsRepository.UpdateAsync(inventoryItem);
        }

        Task itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
        Task inventoryUpdatedTask = context.Publish(
            new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            )
        );

        await Task.WhenAll(inventoryUpdatedTask, itemsGrantedTask);
    }
}