using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core;


{
    /// <summary>
    /// Registers an identity type with the manager.
    /// </summary>
    /// <typeparam name="TIdentity">The identity type to register.</typeparam>
    void RegisterIdentityType<TIdentity>() where TIdentity : Identity;

    /// <summary>
    /// Parses a string representation of an identity into the appropriate identity type.
    /// </summary>
    /// <param name="value">The string representation of the identity.</param>
    /// <returns>The parsed identity instance.</returns>
    Identity Parse(string value);

    /// <summary>
    /// Generates a new identity of the specified type using the counter manager.
    /// </summary>
    /// <typeparam name="TIdentity">The type of identity to generate.</typeparam>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A new identity with the next counter value.</returns>
    Task<TIdentity> GenerateNewAsync<TIdentity>(CancellationToken cancellationToken = default) 
        where TIdentity : Identity;

    /// <summary>
    /// Generates a new identity with the specified prefix using the counter manager.
    /// </summary>
    /// <param name="prefix">The prefix of the identity type to generate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A new identity with the next counter value.</returns>
    Task<Identity> GenerateNewAsync(string prefix, CancellationToken cancellationToken = default);
}
