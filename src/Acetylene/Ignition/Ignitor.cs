using System.Collections;
using System.DirectoryServices;
using System.IO.Abstractions;
using System.Text.Json;
using Acetylene;
using Serilog;

public class Ignitor {
    private readonly IFileSystem _fileSystem;
    
    public Ignitor(IFileSystem fileSystem) {
        _fileSystem = fileSystem;
    }
    
    public Ignitor(): this(new FileSystem()){}

    public static IgnitionFile Parse(string contents) {
        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<IgnitionFile>(contents, options);
    }
    
    public IFileInfo GetIgnitionConfig() {
        var drive = _fileSystem.DriveInfo.GetDrives().GetIgnitionDrive();
        var directory = drive.RootDirectory.GetDirectories("ignition").Single();
        return directory.GetFiles("config.ign").Single();
    }

    public static void CreateUser(string name, string password, List<string> groups) {
        try {
            using var directory = new DirectoryEntry($"WinNT://{Environment.MachineName},computer");
            var newUser = directory.Children.Add(name, "user");
            newUser.Invoke("SetPassword", password);
            newUser.Invoke("Put", "Description", "Acetylene Created User");
            newUser.CommitChanges();
            Log.Information("Successfully created account:" + name);

            foreach (var group in groups) {
                // check if group exists?
                var newGroup = directory.Children.Add(group, "group");
                newGroup.Invoke("Add", newUser.Path);
                newGroup.CommitChanges();

                Log.Information("Successfully created group:" + group);
            }
        }
        catch {
            Log.Error("Encountered error while creating account:" + name);
        }
    }

    public void AddSshKey(List<string> keys, string username) {
        _fileSystem.Directory.CreateDirectory("C:\\ProgramData\\ssh");

        // if config.PrimaryGroup == "Administrators 
        const string path = @"C:\ProgramData\ssh\administrators_authorized_keys";
        // icacls.exe "C:\ProgramData\ssh\administrators_authorized_keys" / inheritance:r / grant "Administrators:F" / grant "SYSTEM:F"
        // else path = @"C:\\Users\\" + username + "\\.ssh";
        // icacls.exe path / inheritance:r / grant "username:F" / grant "SYSTEM:F"
       
        try {
            _fileSystem.File.AppendAllLines(path, keys);

        } catch {
            Log.Error("Encountered error while adding ssh keys for account:" + username);
        }
    }
}