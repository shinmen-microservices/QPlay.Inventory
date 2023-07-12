using System;

namespace QPlay.Inventory.Service.Exceptions;

[Serializable]
public class UnknownItemException : Exception
{
    public UnknownItemException(Guid itemId)
        : base($"Unknown item '{itemId}'")
    {
        ItemId = itemId;
    }

    public Guid ItemId { get; }
}