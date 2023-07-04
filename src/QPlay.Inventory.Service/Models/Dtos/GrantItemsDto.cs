using System;

namespace QPlay.Inventory.Service.Models.Dtos;

public record GrantItemsDto
(
    Guid UserId,
    Guid CatalogItemId,
    int Quantity
);