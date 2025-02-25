namespace NetDaemon.HassClient.Tests.Integration;

public class WebsocketIntegrationTests : IntegrationTestBase
{
    public WebsocketIntegrationTests(HomeAssistantServiceFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TestSuccessfulConnectShouldReturnConnection()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        ctx.HomeAssistantConnection.Should().NotBeNull();
    }

    [Fact]
    public async Task TestGetServicesShouldReturnRawJsonElement()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var services = await ctx.HomeAssistantConnection
            .GetServicesAsync(TokenSource.Token)
            .ConfigureAwait(false);

        services.Should().NotBeNull();
    }

    [Fact]
    public async Task TestCallServiceShouldSucceed()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        await ctx.HomeAssistantConnection
            .CallServiceAsync(
                "domain",
                "service",
                null,
                new HassTarget
                {
                    EntityIds = new[] { "light.test" }
                },
                TokenSource.Token)
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task TestGetDevicesShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var services = await ctx.HomeAssistantConnection
            .GetDevicesAsync(TokenSource.Token)
            .ConfigureAwait(false);

        services.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestGetStatesShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var states = await ctx.HomeAssistantConnection
            .GetStatesAsync(TokenSource.Token)
            .ConfigureAwait(false);

        states.Should().HaveCount(19);
    }

    [Fact]
    public async Task TestUnauthorizedShouldThrowCorrectException()
    {
        var mock = HaFixture.HaMock ?? throw new ApplicationException("Unexpected for the mock server to be null");

        var settings = new HomeAssistantSettings
        {
            Host = "127.0.0.1",
            Port = mock.ServerPort,
            Ssl = false,
            Token = "wrong token"
        };
        await Assert.ThrowsAsync<HomeAssistantConnectionException>(
            async () => await GetConnectedClientContext(settings).ConfigureAwait(false));
    }

    [Fact]
    public async Task TestWrongHostShouldThrowCorrectException()
    {
        var mock = HaFixture.HaMock ?? throw new ApplicationException("Unexpected for the mock server to be null");

        var settings = new HomeAssistantSettings
        {
            Host = "127.0.0.2",
            Port = mock.ServerPort,
            Ssl = false,
            Token = "token does not matter"
        };
        await Assert.ThrowsAsync<WebSocketException>(async () =>
            await GetConnectedClientContext(settings).ConfigureAwait(false));
    }

    [Fact]
    public async Task TestGetEntitiesShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var entities = await ctx.HomeAssistantConnection
            .GetEntitiesAsync(TokenSource.Token)
            .ConfigureAwait(false);

        entities.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestGetConfigShouldReturnCorrectInformation()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var config = await ctx.HomeAssistantConnection
            .GetConfigAsync(TokenSource.Token)
            .ConfigureAwait(false);

        config
            .Should()
            .NotBeNull();
        config.Latitude
            .Should()
            .Be(63.1394549f);
    }

    [Fact]
    public async Task TestPing()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        await ctx.HomeAssistantConnection
            .PingAsync(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), TokenSource.Token)
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task TestGetAreasShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var services = await ctx.HomeAssistantConnection
            .GetAreasAsync(TokenSource.Token)
            .ConfigureAwait(false);

        services.Should().HaveCount(3);
    }

    [Fact]
    public async Task TestErrorReturnShouldThrowException()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await ctx.HomeAssistantConnection
            .SendCommandAndReturnResponseAsync<SimpleCommand, object?>(
                new SimpleCommand("fake_return_error"),
                TokenSource.Token
            )
            .ConfigureAwait(false));
    }

    [Fact]
    public async Task TestSubscribeAndGetEvent()
    {
        using CancellationTokenSource tokenSource = new(TestSettings.DefaultTimeout + 1000);
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var subscribeTask = ctx.HomeAssistantConnection
            .OnHomeAssistantEvent
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(HassEvent?)))
            .FirstAsync()
            .ToTask();

        _ = ctx.HomeAssistantConnection
            .ProcessHomeAssistantEventsAsync(tokenSource.Token);

        var haEvent = await subscribeTask.ConfigureAwait(false);

        haEvent.Should().NotBeNull();
        haEvent?
            .EventType
            .Should()
            .BeEquivalentTo("state_changed");

        // We test the state changed 
        var changedEvent = haEvent?.ToStateChangedEvent();
        changedEvent?.EntityId
            .Should()
            .BeEquivalentTo("binary_sensor.vardagsrum_pir");

        var attr = changedEvent?.NewState?.AttributesAs<AttributeTest>();
        attr?
            .BatteryLevel
            .Should()
            .Be(100);

        changedEvent?.NewState?.Attributes?.FirstOrDefault(n => n.Key == "battery_level")
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task TestGetServiceEvent()
    {
        using CancellationTokenSource tokenSource = new(TestSettings.DefaultTimeout + 1000);
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var subscribeTask = ctx.HomeAssistantConnection
            .OnHomeAssistantEvent
            .Where(n => n.EventType == "call_service")
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(HassEvent?)))
            .FirstAsync()
            .ToTask();

        _ = ctx.HomeAssistantConnection
            .ProcessHomeAssistantEventsAsync(tokenSource.Token);

        await ctx.HomeAssistantConnection
            .SendCommandAndReturnResponseAsync<SimpleCommand, object?>(
                new SimpleCommand("fake_service_event"),
                TokenSource.Token
            )
            .ConfigureAwait(false);

        var haEvent = await subscribeTask.ConfigureAwait(false);

        haEvent.Should().NotBeNull();
        haEvent?
            .EventType
            .Should()
            .BeEquivalentTo("call_service");
        // Test the conversion to service event
        var stateChangedEvent = haEvent?.ToCallServiceEvent();
        stateChangedEvent?.Domain
            .Should()
            .BeEquivalentTo("light");
    }

    [Fact]
    public async Task TestStaticConnectApi()
    {
        var port = HaFixture.HaMock?.ServerPort ?? throw new InvalidOperationException();
        var tokenSource = new CancellationTokenSource(5000);
        await using var connection = await HomeAssistantClientConnector
            .ConnectClientAsync("127.0.0.1", port, false, "ABCDEFGHIJKLMNOPQ", tokenSource.Token)
            .ConfigureAwait(false);

        Assert.NotNull(connection);

        // We just check any use-case to make sure we can communicate
        var states = await connection
            .GetStatesAsync(TokenSource.Token)
            .ConfigureAwait(false);

        states.Should().HaveCount(19);
    }

    private record AttributeTest
    {
        [JsonPropertyName("battery_level")] public int BatteryLevel { get; init; }
    }
}
