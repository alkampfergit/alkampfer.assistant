namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Base entity class with strongly-typed identity.
/// </summary>
/// <typeparam name="TId">The identity type, must inherit from <see cref="Identity"/>.</typeparam>
public abstract class BaseEntity<TId> where TId : Identity
{
    public TId Id { get; set; } = default!;
}
