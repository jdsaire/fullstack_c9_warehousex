namespace WarehouseX.Exceptions;

/// <summary>
/// Thrown when an order payload or identifier is invalid or corrupted.
/// </summary>
public class InvalidOrderException : WarehouseException
{
    public InvalidOrderException(string message) : base(message) { }
}
