namespace AuctionService.Shared.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resource, Guid id) : base($"{resource} '{id}' was not found.") { }
}
