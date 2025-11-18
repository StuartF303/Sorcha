namespace Sorcha.Wallet.Service.Api.Tests;

public class WalletEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WalletEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateWallet_ReturnsCreated_WithMnemonic()
    {
        var request = new CreateWalletRequest
        {
            Name = "API Test Wallet",
            Algorithm = "ED25519",
            WordCount = 12
        };

        var response = await _client.PostAsJsonAsync("/api/v1/wallets", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CreateWalletResponse>();
        payload.Should().NotBeNull();
        payload!.Wallet.Address.Should().NotBeNullOrEmpty();
        payload.MnemonicWords.Should().HaveCount(12);
    }

    [Fact]
    public async Task GetWallet_ReturnsNotFound_ForUnknown()
    {
        var response = await _client.GetAsync("/api/v1/wallets/ws1-unknown");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
