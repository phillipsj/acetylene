using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Acetylene.Tests; 

public class ExtensionsTests {
    [Fact]
    public void ShouldFindDriveWithCombustionLabel() {
        // Arrange
        var drives = new IDriveInfo[2] {
            new MockDriveInfo(new MockFileSystem(), "c"),
            new MockDriveInfo(new MockFileSystem(), "d") {
                VolumeLabel = "combustion"
            }
        };
        
        // Act-Assert
        drives.GetCombustionDrive().Name.Should().Be(@"d:\");
    }
    
    [Fact]
    public void ShouldFindDriveWithCombustionWithIgnitionLabel() {
        // Arrange
        var drives = new IDriveInfo[2] {
            new MockDriveInfo(new MockFileSystem(), "c"),
            new MockDriveInfo(new MockFileSystem(), "d") {
                VolumeLabel = "ignition"
            }
        };
        
        // Act-Assert
        drives.GetCombustionDrive().Name.Should().Be(@"d:\");
    }
    
    [Fact]
    public void ShouldErrorWithoutCombustionLabel() {
        // Arrange
        var drives = new IDriveInfo[2] {
            new MockDriveInfo(new MockFileSystem(), "c"),
            new MockDriveInfo(new MockFileSystem(), "d") 
        };
        
        // Act
        var ex = Record.Exception(() => drives.GetCombustionDrive().Name.Should().Be(@"d:\"));
        
        // Assert
        ex.Should().BeOfType<InvalidOperationException>();
    }
    
    [Fact]
    public void ShouldFindDriveWithIgnitionLabel() {
        // Arrange
        var drives = new IDriveInfo[2] {
            new MockDriveInfo(new MockFileSystem(), "c"),
            new MockDriveInfo(new MockFileSystem(), "d") {
                VolumeLabel = "ignition"
            }
        };
        
        // Act-Assert
        drives.GetIgnitionDrive().Name.Should().Be(@"d:\");
    }
    
    [Fact]
    public void ShouldErrorWithoutIgnitionLabel() {
        // Arrange
        var drives = new IDriveInfo[2] {
            new MockDriveInfo(new MockFileSystem(), "c"),
            new MockDriveInfo(new MockFileSystem(), "d") 
        };
        
        // Act
        var ex = Record.Exception(() => drives.GetIgnitionDrive().Name.Should().Be(@"d:\"));
        
        // Assert
        ex.Should().BeOfType<InvalidOperationException>();
    }
}