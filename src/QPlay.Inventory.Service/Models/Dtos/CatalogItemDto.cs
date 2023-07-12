using System;

namespace QPlay.Inventory.Service.Models.Dtos;

public record CatalogItemDto(Guid Id, string Name, string Description);