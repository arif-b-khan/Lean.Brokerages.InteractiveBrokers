using FluentAssertions;
using QuantConnect.InteractiveBrokers.ToolBox.Security;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public async Task LoadConfig_HydratesMissingValuesFromCredentialStore()
    {
        var store = new InMemoryCredentialStore(new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "stored-user",
            ["IB_PASSWORD"] = "stored-password",
            ["IB_ACCOUNT"] = "stored-account"
        });

        var loader = new ConfigLoader(logger: null, credentialStore: store);

        var config = await loader.LoadConfig();

        config["IB_USERNAME"].Should().Be("stored-user");
        config["IB_PASSWORD"].Should().Be("stored-password");
        config["IB_ACCOUNT"].Should().Be("stored-account");
    }

    [Fact]
    public async Task PersistSecretsAsync_SavesValuesToCredentialStore()
    {
        var backingStore = new Dictionary<string, string>();
        var store = new InMemoryCredentialStore(backingStore);
        var loader = new ConfigLoader(logger: null, credentialStore: store);

        var config = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "user",
            ["IB_PASSWORD"] = "password",
            ["IB_ACCOUNT"] = "account"
        };

        await loader.PersistSecretsAsync(config);

        backingStore.Should().ContainKey("IB_USERNAME").WhoseValue.Should().Be("user");
        backingStore.Should().ContainKey("IB_PASSWORD").WhoseValue.Should().Be("password");
        backingStore.Should().ContainKey("IB_ACCOUNT").WhoseValue.Should().Be("account");
    }

    [Fact]
    public async Task PersistSecretsAsync_RemovesMissingValues()
    {
        var backingStore = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "user",
            ["IB_PASSWORD"] = "password",
            ["IB_ACCOUNT"] = "account"
        };
        var store = new InMemoryCredentialStore(backingStore);
        var loader = new ConfigLoader(logger: null, credentialStore: store);

        var config = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "user"
        };

        await loader.PersistSecretsAsync(config);

        backingStore.Should().ContainKey("IB_USERNAME");
        backingStore.Should().NotContainKey("IB_PASSWORD");
        backingStore.Should().NotContainKey("IB_ACCOUNT");
    }

    private sealed class InMemoryCredentialStore : ICredentialStore
    {
        private readonly Dictionary<string, string> _backingStore;

        public InMemoryCredentialStore(Dictionary<string, string> backingStore)
        {
            _backingStore = backingStore;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            _backingStore.Remove(key);
            return Task.CompletedTask;
        }

        public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            _backingStore.TryGetValue(key, out var value);
            return Task.FromResult<string?>(value);
        }

        public Task SaveAsync(string key, string secret, CancellationToken cancellationToken = default)
        {
            _backingStore[key] = secret;
            return Task.CompletedTask;
        }
    }
}
