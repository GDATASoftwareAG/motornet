using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Motor.Extensions.TestUtilities;
using AspNetExample;
using AspNetExample.Models;
using NSubstitute;
using Xunit;
using Assert = Xunit.Assert;

namespace AspNetExample__IntegrationTest;

[Collection("Sequential")]
public class CustomerControllerTests
{
    private const string Endpoint = "/api/v1/customer";

    [Fact]
    public async Task GetCustomers_Uninitialized_Empty()
    {
        var httpClient = new MotorTestServerBuilder<Startup>()
            .Build()
            .CreateClient();

        var result = await httpClient.GetAsync(Endpoint);
        var response = await result.Content.ReadFromJsonAsync<IList<Customer>>();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(17)]
    [InlineData(-3)]
    [InlineData(18)]
    [InlineData(42)]
    public async Task CreateCustomer_DenyAllValidator_BadRequest(long age)
    {
        var customer = new Customer { Age = age };
        var httpClient = new MotorTestServerBuilder<Startup>()
            .SubstituteSingleton<ICustomerValidator>(validator =>
                validator.Validate(Arg.Any<Customer>()).Returns(false))
            .Build()
            .CreateClient();

        var result = await httpClient.PostAsJsonAsync(Endpoint, customer);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(17)]
    [InlineData(-3)]
    [InlineData(18)]
    [InlineData(42)]
    public async Task CreateCustomer_AllowAllValidator_Created(long age)
    {
        var customer = new Customer { Age = age };
        var httpClient = new MotorTestServerBuilder<Startup>()
            .SubstituteSingleton<ICustomerValidator>(validator =>
                validator.Validate(Arg.Any<Customer>()).Returns(true))
            .Build()
            .CreateClient();

        var result = await httpClient.PostAsJsonAsync(Endpoint, customer);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
    }
}
