using System;

namespace QPlay.Inventory.Service.Models.Dtos;

public record InventoryItemDto(
    Guid CatalogItemId,
    string Name,
    string Description,
    int Quantity,
    DateTimeOffset AcquiredDate
);