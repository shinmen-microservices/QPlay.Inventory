using MassTransit;
using QPlay.Common.Repositories.Interfaces;
using QPlay.Inventory.Contracts;
using QPlay.Inventory.Service.Exceptions;
using QPlay.Inventory.Service.Models.Entities;
using System.Threading.Tasks;

namespace QPlay.Inventory.Service.Consumers;

public class SubtractItemsConsumer : IConsumer<SubtractItems>
{
    private readonly IRepository<CatalogItem> catalogItemsRepository;
    private readonly IRepository<InventoryItem> inventoryItemsRepository;

    public SubtractItemsConsumer(
        IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository
    )
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
    }

    public async Task Consume(ConsumeContext<SubtractItems> context)
    {
        SubtractItems message = context.Message;

        CatalogItem catalogItem =
            await catalogItemsRepository.GetAsync(message.CatalogItemId)
            ?? throw new UnknownItemException(message.CatalogItemId);

        var inventoryItem = await inventoryItemsRepository.GetAsync(
            item => item.UserId == message.UserId && item.CatalogItemId == catalogItem.Id
        );

        if (inventoryItem != null)
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
                return;
            }

            inventoryItem.Quantity -= message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await inventoryItemsRepository.UpdateAsync(inventoryItem);

            await context.Publish(
                new InventoryItemUpdated(
                    inventoryItem.UserId,
                    inventoryItem.CatalogItemId,
                    inventoryItem.Quantity
                )
            );
        }

        await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
    }
}