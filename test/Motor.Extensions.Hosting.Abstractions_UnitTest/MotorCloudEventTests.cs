using System;
using System.Collections.Generic;
using Motor.Extensions.Hosting.Abstractions;
using Moq;
using Xunit;

namespace Motor.Extensions.Hosting.Abstractions_UnitTest
{
    public class MotorCloudEventTests
    {
        [Fact]
        public void CreateNew_UseFromOld_IdAndDateAreNewCreated()
        {
            var oldEvent = new MotorCloudEvent<string>(GetApplicationNameService("test://non2"), " ", "String", new Uri("test://non"));
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
            var oldEvent = new MotorCloudEvent<string>(GetApplicationNameService(), " ", "String", new Uri("test://non"));
            var expectedData = new List<string>();
            
            var newEvent = oldEvent.CreateNew(expectedData, true);

            Assert.Equal(oldEvent.Id, newEvent.Id);
            Assert.Equal(oldEvent.Time, newEvent.Time);
            Assert.Equal(oldEvent.Type, newEvent.Type);
            Assert.Equal(oldEvent.Source, newEvent.Source);
        }
        
        [Fact]
        public void CreateNew_UseFromOld_DataIsUpdated()
        {
            var oldEvent = new MotorCloudEvent<string>(GetApplicationNameService("test://non2"), " ", "String", new Uri("test://non"));
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
}
