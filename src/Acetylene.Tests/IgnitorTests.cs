using System.IO.Abstractions;
using System.ServiceProcess;
using Acetylene.Ignition;
using Moq;

namespace Acetylene.Tests {
    public class IgnitorTests {
        [Fact]
        public void ShouldParseIgnitionFile() {
            // Arrange
            const string ignitionFile = @"{
  ""ignition"": { ""version"": ""3.3.0"" },
  ""passwd"": {
    ""users"": [
        {
        ""name"": ""root"",
        ""passwordHash"": ""pemFK1OejzrTI""
        }
    ]
  }
}
";
 
            // Act
            var result = Ignitor.Parse(ignitionFile);

            // Assert
            result.Ignition?.Version.Should().Be("3.3.0");
            result.Passwd?.Users.Should().HaveCount(1);
        }

        [Fact]
        public void ShouldGetIgnitionConfig() {
            // Arrange
            var configInfo = new Mock<IFileInfo>();
            configInfo.Setup(s => s.Name).Returns("config.ign");
        
            var ignitionDirectory = new Mock<IDirectoryInfo>();
            ignitionDirectory.Setup(c => c.Name).Returns("ignition");
            ignitionDirectory.Setup(d => d.GetFiles(It.IsAny<string>())).Returns(new[] {
                configInfo.Object
            });
            
            var directoryInfo = new Mock<IDirectoryInfo>();
            directoryInfo.Setup(d => d.GetDirectories(It.IsAny<string>())).Returns(new[] {
                ignitionDirectory.Object
            });
            
   
            var driveInfo = new Mock<IDriveInfo>();
            driveInfo.Setup(d => d.RootDirectory).Returns(directoryInfo.Object);
            driveInfo.Setup(d => d.IsReady).Returns(true);
            driveInfo.Setup(d => d.VolumeLabel).Returns("ignition");
        
            var driveInfoFactory = new Mock<IDriveInfoFactory>();
            driveInfoFactory.Setup(x => x.GetDrives()).Returns(new[] {
                driveInfo.Object, 
            });
        
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.DriveInfo).Returns(driveInfoFactory.Object);
        
            var ignitor = new Ignitor(fileSystem.Object);
            var config = ignitor.GetIgnitionConfig();
            
            // Assert
            config.Name.Should().Be("config.ign");
        }

        [Fact]
        public void ShouldProcessSystemDUnits() {
            // Arrange
            var dockerService = new Mock<IServiceController>();
            dockerService.Setup(x => x.ServiceName).Returns("docker");
            var sshd = new Mock<IServiceController>();
            sshd.Setup(x => x.ServiceName).Returns("sshd");
            
            var controller = new Mock<IServiceController>();
            controller.Setup(x => x.GetServices()).Returns(new[] { dockerService.Object, sshd.Object });
            
            var ignitor = new Ignitor(controller.Object);
            var units = new List<Unit> {
                new() {
                    Name = "docker",
                    Enabled = false
                },
                new() {
                    Name = "sshd",
                    Enabled = true
                }
            };
            
            // Act
            ignitor.ProcessServices(units);
            
            // Assert
            controller.Verify(x => x.GetServices(), Times.AtLeastOnce);
            
            dockerService.VerifyGet(x=> x.ServiceName);
            dockerService.Verify(x=>x.Stop(), Times.AtLeastOnce);
            
            sshd.VerifyGet(x=>x.ServiceName, Times.AtLeastOnce);
            sshd.Verify(x=>x.Start(), Times.AtLeastOnce);
        }
    }
}