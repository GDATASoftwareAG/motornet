using System.Threading.Tasks;
using Motor.Extensions.TestUtilities;
using NSubstitute;
using Xunit;

namespace Motor.Extensions.Hosting.AspNet_IntegrationTest;

public class FakeHostingTest
{
    private const string Endpoint = "/randomNumber";

    [Fact]
    public async Task Get_ActualRandom_SuccessStatusCode()
    {
        var httpClient = MotorTestHost.BasedOn<ActualStartup>()
            .Build()
            .CreateClient();

        var response = await httpClient.GetAsync(Endpoint);

        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [Fact]
    public async Task Get_FakeRandom_SuccessStatusCode()
    {
        var httpClient = MotorTestHost.BasedOn<ActualStartup>()
            .SubstituteTransient<IRandomNumberGenerator>(generator => generator.Next().Returns(0))
            .Build()
            .CreateClient();

        var response = await httpClient.GetAsync(Endpoint);

        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [Fact]
    public async Task Get_ActualRandom_NumberResponse()
    {
        var httpClient = MotorTestHost.BasedOn<ActualStartup>()
            .Build()
            .CreateClient();

        var response = await httpClient.GetAsync(Endpoint);

        var stringContent = await response.Content.ReadAsStringAsync();
        Assert.True(int.TryParse(stringContent, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public async Task Get_FakeRandom_FakedResponse(int expectedResponse)
    {
        var httpClient = MotorTestHost.BasedOn<ActualStartup>()
            .SubstituteTransient<IRandomNumberGenerator>(generator => generator.Next().Returns(expectedResponse))
            .Build()
            .CreateClient();

        var response = await httpClient.GetAsync(Endpoint);

        var stringContent = await response.Content.ReadAsStringAsync();
        Assert.True(int.TryParse(stringContent, out var actualResponse));
        Assert.Equal(expectedResponse, actualResponse);
    }
}
