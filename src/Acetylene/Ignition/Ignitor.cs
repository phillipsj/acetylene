using System.DirectoryServices;
using System.IO.Abstractions;
using System.Text.Json;
using Acetylene;
using Acetylene.Ignition;
using Microsoft.Win32;
using Serilog;
using Serilog.Configuration;

public class Ignitor {
    private readonly IFileSystem _fileSystem;
    private readonly IServiceController _serviceController;

    private const string ActiveComputerKey = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ActiveComputerName";
    private const string ComputerNameKey = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName";
    private const string TcpipParametersKey = "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters\\";
    
    public Ignitor(IFileSystem fileSystem, IServiceController serviceController) {
        _fileSystem = fileSystem;
        _serviceController = serviceController;
    }
    
    public Ignitor(IFileSystem fileSystem): this(fileSystem, new IgnitionServiceController()){}
    
    public Ignitor(IServiceController serviceController): this(new FileSystem(), serviceController){}
    
    public Ignitor(): this(new FileSystem(), new IgnitionServiceController()){}

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

        } catch(Exception e) {
            Log.Error(e,"Encountered error while adding ssh keys for account:" + username);
        }
    }

    public void ProcessServices(List<Unit> units) {
        var services = _serviceController.GetServices();
        units.ForEach(x => {
            var service = services.FirstOrDefault(s => s?.ServiceName == x.Name, null);
            if (service is null) return;
            if (x.Enabled) {
                service.Start();
            }
            else {
                service.Stop();
            }
        });
    }

    public void ProcessFiles(List<File> files) {
        files.ForEach(x => {
            if (x.Path == "/etc/hostname") {
                
            }
            if (_fileSystem.File.Exists(x.Path)) {
                _fileSystem.File.AppendAllText(x.Path, x.Contents?.Source);
            }
            else {
                _fileSystem.File.WriteAllText(x.Path, x.Contents?.Source);
            }
        });
    }
    
    public static bool SetHostName(string name)
    {
        try {
            var key = Registry.LocalMachine;
            using var activeKey = key.CreateSubKey(ActiveComputerKey);
            activeKey.SetValue("ComputerName", name);
            activeKey.Close();
            using var computerKey = key.CreateSubKey(ComputerNameKey);
            computerKey.SetValue("ComputerName", name);
            computerKey.Close();
            using var tcpipKey = key.CreateSubKey(TcpipParametersKey);
            tcpipKey.SetValue("Hostname",name);
            tcpipKey.SetValue("NV Hostname",name);
            tcpipKey.Close(); 
            Log.Information("Hostname has been set to: {0}", name);
            return true;
        }
        catch (Exception e) {
            Log.Error(e, "Encountered error while setting hostname.");
            return false;
        }
    }
}