using PersianAds.Models;

namespace PersianAds.Abstractions;

public interface IAdNetworkService
{
    string NetworkName { get; }

    bool IsInitialized { get; }

    Task InitializeAsync(AdNetworkInitializationOptions options, CancellationToken cancellationToken = default);
}
