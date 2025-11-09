namespace Alkampfer.Assistant.Playground;

/// <summary>
/// Base class for all playground examples.
/// </summary>
public abstract class ExampleBase
{
    /// <summary>
    /// Gets the name of the example that will be displayed in the menu.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the category of the example. Examples with the same category will be grouped together.
    /// Return null or empty string for examples that should appear at the root level.
    /// </summary>
    public virtual string? Category => null;

    /// <summary>
    /// Gets the description of the example.
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    /// Executes the example.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}
