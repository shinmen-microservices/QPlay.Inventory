using QPlay.Inventory.Service.Models.Dtos;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace QPlay.Inventory.Service.Clients;

public class CatalogClient
{
    private readonly HttpClient httpClient;

    public CatalogClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetCatalogItemsAsync()
    {
        IReadOnlyCollection<CatalogItemDto> items = await httpClient.GetFromJsonAsync<IReadOnlyCollection<CatalogItemDto>>("/items");
        return items;
    }
}
