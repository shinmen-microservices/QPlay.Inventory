using MassTransit;
using QPlay.Catalog.Contracts;
using QPlay.Common.Repositories.Interfaces;
using QPlay.Inventory.Service.Models.Entities;
using System.Threading.Tasks;

namespace QPlay.Inventory.Service.Consumers;

public class CatalogItemCreatedConsumer : IConsumer<CatalogItemCreated>
{
    private readonly IRepository<CatalogItem> repository;

    public CatalogItemCreatedConsumer(IRepository<CatalogItem> repository)
    {
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<CatalogItemCreated> context)
    {
        CatalogItemCreated message = context.Message;
        CatalogItem item = await repository.GetAsync(message.ItemId);

        if (item != null) return;

        item = new()
        {
            Id = message.ItemId,
            Name = message.Name,
            Description = message.Description
        };

        await repository.CreateAsync(item);
    }
}
