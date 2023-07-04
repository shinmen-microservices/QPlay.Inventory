using QPlay.Inventory.Service.Entities;
using QPlay.Inventory.Service.Models.Dtos;

namespace QPlay.Inventory.Service.Extensions;

public static class DtoExtensions
{
    public static InventoryItemDto AsDto(this InventoryItem item, string name, string description)
    {
        return new InventoryItemDto
        (
            item.CatalogItemId,
            name,
            description,
            item.Quantity,
            item.AcquiredDate
        );
    }
}