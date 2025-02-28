using System.Threading.Tasks;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.AspNet_IntegrationTest;

public class DefaultHostingTest(MotorHostApplicationFactory<ActualStartup> factory)
    : IClassFixture<MotorHostApplicationFactory<ActualStartup>>
{
    private const string Endpoint = "/randomNumber";

    [Fact]
    public async Task Get_RandomEndpoint_SuccessStatusCode()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(Endpoint);

        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [Fact]
    public async Task Get_RandomEndpoint_NumberResponse()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(Endpoint);

        var stringContent = await response.Content.ReadAsStringAsync();
        Assert.True(int.TryParse(stringContent, out _));
    }
}
