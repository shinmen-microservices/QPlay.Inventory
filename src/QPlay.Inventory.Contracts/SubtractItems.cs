using System;

namespace QPlay.Inventory.Contracts;

public record SubtractItems
(
    Guid UserId,
    Guid CatalogItemId,
    int Quantity,
    Guid CorrelationId
);
