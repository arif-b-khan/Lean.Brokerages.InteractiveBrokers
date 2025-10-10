using FluentAssertions;
using QuantConnect.InteractiveBrokers.ToolBox.Security;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests.Security;

public sealed class CredentialStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public CredentialStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task SaveAndLoad_ReturnsOriginalSecret()
    {
        var store = new CredentialStore(_tempDirectory);

        await store.SaveAsync("PrimaryKey", "super-secret");

        var retrieved = await store.LoadAsync("PrimaryKey");
        retrieved.Should().Be("super-secret");
    }

    [Fact]
    public async Task SaveOverwritesExistingValue()
    {
        var store = new CredentialStore(_tempDirectory);

        await store.SaveAsync("ApiKey", "first-value");
        await store.SaveAsync("ApiKey", "second-value");

        var retrieved = await store.LoadAsync("ApiKey");
        retrieved.Should().Be("second-value");
    }

    [Fact]
    public async Task LoadReturnsNullWhenKeyMissing()
    {
        var store = new CredentialStore(_tempDirectory);
        var retrieved = await store.LoadAsync("Missing");
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRemovesPersistedSecret()
    {
        var store = new CredentialStore(_tempDirectory);
        await store.SaveAsync("ApiSecret", "value");

        await store.DeleteAsync("ApiSecret");

        var fileCount = Directory.Exists(_tempDirectory)
            ? Directory.GetFiles(_tempDirectory).Length
            : 0;
        fileCount.Should().Be(0);
        (await store.LoadAsync("ApiSecret")).Should().BeNull();
    }

    [Fact]
    public async Task SavedPayloadIsNotPlainTextOnDisk()
    {
        var store = new CredentialStore(_tempDirectory);
        await store.SaveAsync("PlainTextCheck", "visible-text");

        var filePath = Directory.GetFiles(_tempDirectory).Single();
        var rawBytes = await File.ReadAllBytesAsync(filePath);
    rawBytes.Should().NotBeEquivalentTo(System.Text.Encoding.UTF8.GetBytes("visible-text"));
    }

    [Fact]
    public async Task KeysAreSanitizedToValidFilenames()
    {
        var store = new CredentialStore(_tempDirectory);
        const string keyWithInvalidChars = "Prod:/Account\\Secret";

        await store.SaveAsync(keyWithInvalidChars, "value");

        var files = Directory.GetFiles(_tempDirectory);
        files.Should().HaveCount(1);
    Path.GetFileName(files[0]).Should().Be("Prod_Account_Secret.bin");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup failures in temp directory
        }
    }
}
