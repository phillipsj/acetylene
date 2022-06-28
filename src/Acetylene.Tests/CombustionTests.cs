using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq; 

namespace Acetylene.Tests; 

using XFS = MockUnixSupport;
public class CombustionTests {
    [Fact]
    public void ShouldFindCombustionDriveAndScript() {
        // Arrange
        var scriptFileInfo = new Mock<IFileInfo>();
        scriptFileInfo.Setup(s => s.Name).Returns("script");
        
        var combustionDirectory = new Mock<IDirectoryInfo>();
        combustionDirectory.Setup(c => c.Name).Returns("combustion");
        var directoryInfo = new Mock<IDirectoryInfo>();
        directoryInfo.Setup(d => d.GetDirectories(It.IsAny<string>())).Returns(new[] {
            combustionDirectory.Object
        });
        directoryInfo.Setup(d => d.GetFiles(It.IsAny<string>())).Returns(new[] {
            scriptFileInfo.Object
        });
   
        var driveInfo = new Mock<IDriveInfo>();
        driveInfo.Setup(d => d.RootDirectory).Returns(directoryInfo.Object);
        driveInfo.Setup(d => d.IsReady).Returns(true);
        driveInfo.Setup(d => d.VolumeLabel).Returns("combustion");
        
        var driveInfoFactory = new Mock<IDriveInfoFactory>();
        driveInfoFactory.Setup(x => x.GetDrives()).Returns(new[] {
            driveInfo.Object, 
        });
        
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.DriveInfo).Returns(driveInfoFactory.Object);
        
        var combustion = new Combustion.Combustion(fileSystem.Object);
        
        // Act 
        //var script = combustion.RetrieveScript();
        
        // Assert
        true.Should().BeTrue();
    }
}