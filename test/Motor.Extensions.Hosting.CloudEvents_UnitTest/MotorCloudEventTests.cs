using System;
using System.Collections.Generic;
using Moq;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Xunit;

namespace Motor.Extensions.Hosting.CloudEvents_UnitTest;

public class MotorCloudEventTests
{
    [Fact]
    public void CreateNew_UseFromOld_IdAndDateAreNewCreated()
    {
        var oldEvent = new MotorCloudEvent<string>(GetApplicationNameService("test://non2"), " ",
            new Uri("test://non"));
        var expectedData = new List<string>();

        var newEvent = oldEvent.CreateNew(expectedData);

        Assert.NotEqual(oldEvent.Id, newEvent.Id);
        Assert.NotEqual(oldEvent.Time, newEvent.Time);
        Assert.NotEqual(oldEvent.Type, newEvent.Type);
        Assert.NotEqual(oldEvent.Source, newEvent.Source);
    }

    [Fact]
    public void CreateNew_UseOldIdentifierFromOld_IdAndDateAreNewCreated()
    {
        var oldEvent =
            new MotorCloudEvent<string>(GetApplicationNameService(), " ", new Uri("test://non"));
        var expectedData = new List<string>();

        var newEvent = oldEvent.CreateNew(expectedData, true);

        Assert.Equal(oldEvent.Id, newEvent.Id);
        Assert.Equal(oldEvent.Time, newEvent.Time);
        Assert.Equal(oldEvent.Source, newEvent.Source);
    }

    [Fact]
    public void CreateNew_UseOldIdentifierFromOld_TypeMatchesOldDataType()
    {
        var oldEvent =
            new MotorCloudEvent<string>(GetApplicationNameService(), " ", new Uri("test://non"));
        var expectedData = new List<string>();

        var newEvent = oldEvent.CreateNew(expectedData, true);

        Assert.Equal(nameof(String), oldEvent.Type);
        Assert.Equal(nameof(String), newEvent.Type);
    }

    [Fact]
    public void CreateNew_OldEventHasContentEncoding_NewEventHasSameContentEncoding()
    {
        var oldEvent =
            new MotorCloudEvent<string>(GetApplicationNameService(), " ", new Uri("test://non"));
        oldEvent.SetEncoding("some-encoding");
        var expectedData = new List<string>();

        var newEvent = oldEvent.CreateNew(expectedData);

        Assert.Equal(oldEvent.GetEncoding(), newEvent.GetEncoding());
    }

    [Fact]
    public void CreateNew_DoNotUseOldIdentifierFromOld_TypeMatchesNewDataType()
    {
        var oldEvent =
            new MotorCloudEvent<string>(GetApplicationNameService(), " ", new Uri("test://non"));
        var expectedData = new List<string>();

        var newEvent = oldEvent.CreateNew(expectedData, false);

        Assert.Equal(nameof(String), oldEvent.Type);
        Assert.Equal(typeof(List<string>).Name, newEvent.Type);
    }

    [Fact]
    public void CreateNew_UseFromOld_DataIsUpdated()
    {
        var oldEvent = new MotorCloudEvent<string>(GetApplicationNameService("test://non2"), " ",
            new Uri("test://non"));
        var expectedData = new List<string>();

        var newEvent = oldEvent.CreateNew(expectedData);

        Assert.Equal(expectedData, newEvent.Data);
    }

    private IApplicationNameService GetApplicationNameService(string source = "test://non")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        return mock.Object;
    }
}
