using System.Threading.Tasks;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.AspNet_IntegrationTest;

public class DefaultHostingTest
{
    private const string Endpoint = "/randomNumber";

    [Fact]
    public async Task Get_RandomEndpoint_SuccessStatusCode()
    {
        await using var server = MotorTestHost.BasedOn<ActualStartup>().Build();
        var client = server.CreateClient();

        var response = await client.GetAsync(Endpoint);

        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [Fact]
    public async Task Get_RandomEndpoint_NumberResponse()
    {
        await using var server = MotorTestHost.BasedOn<ActualStartup>().Build();
        var client = server.CreateClient();

        var response = await client.GetAsync(Endpoint);

        var stringContent = await response.Content.ReadAsStringAsync();
        Assert.True(int.TryParse(stringContent, out _));
    }
}
