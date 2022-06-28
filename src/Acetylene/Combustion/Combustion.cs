using System.IO.Abstractions;

namespace Acetylene.Combustion; 

public class Combustion {
    private readonly IFileSystem _fileSystem;
    
    public Combustion(IFileSystem fileSystem) {
        _fileSystem = fileSystem;
    }
    
    public Combustion() : this(new FileSystem()){}

    public IFileInfo RetrieveScript() {
       var drive = _fileSystem.DriveInfo.GetDrives().GetCombustionDrive();
       var directory = drive.RootDirectory.GetDirectories("combustion").Single();
       return directory.GetFiles("script").Single();
    }
}