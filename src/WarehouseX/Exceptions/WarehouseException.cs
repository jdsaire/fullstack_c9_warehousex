using System;

namespace WarehouseX.Exceptions;

/// <summary>
/// Base domain exception for WarehouseX system failures and business rule violations.
/// </summary>
public abstract class WarehouseException : Exception
{
    protected WarehouseException(string message) : base(message) { }
    protected WarehouseException(string message, Exception innerException) : base(message, innerException) { }
}
