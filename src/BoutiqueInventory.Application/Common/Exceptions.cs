namespace BoutiqueInventory.Application.Common;

/// <summary>Thrown when a requested resource cannot be located.</summary>
public sealed class NotFoundException : Exception
{
    public string ResourceType { get; }
    public object Identifier { get; }

    public NotFoundException(string resourceType, object identifier)
        : base($"{resourceType} with identifier '{identifier}' was not found.")
    {
        ResourceType = resourceType;
        Identifier = identifier;
    }
}

/// <summary>Thrown when an operation violates a uniqueness or state rule.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Thrown when caller-supplied data fails domain rules.</summary>
public sealed class DomainValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            [string.Empty] = new[] { message }
        };
    }

    public DomainValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
