using System.Collections;
using System.DirectoryServices;
using System.Text.Json;

public class Ignitor {
    public Ignitor() {
    }

    public static IgnitionFile Parse(string contents) {
        var options = new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<IgnitionFile>(contents, options);
    }

    public static void CreateUser(string name, string password, IList groups) {
        try {
            // check if user exists?
            DirectoryEntry NewUser = new DirectoryEntry("WinNT://" +
                                Environment.MachineName + ",computer").Children.Add(name, "user");
            NewUser.Invoke("SetPassword", new object[] { password });
            NewUser.Invoke("Put", new object[] { "Description", "Acetylene Created User" });
            NewUser.CommitChanges();
            Serilog.Log.Debug("Successfully created account:" + name);

            foreach (var group in groups) {
                // check if group exists?
                DirectoryEntry NewGroup = new DirectoryEntry("WinNT://" +
                    Environment.MachineName + ",computer").Children.Add((string)group, "group");
                if (NewGroup != null) {
                    NewGroup.Invoke("Add", new object[] { NewUser.Path.ToString() });
                }
                NewGroup.CommitChanges();
                Serilog.Log.Debug("Successfully created group:" + group);
            }
        } catch {
            Serilog.Log.Error("Encountered error while creating account:" + name);
        }
    }

    public static void AddSSHKey(IList keys, string username) {
        Directory.CreateDirectory("C:\\ProgramData\\ssh");

        // if config.PrimaryGroup == "Administrators 
        string path = @"C:\ProgramData\ssh\administrators_authorized_keys";
        // icacls.exe "C:\ProgramData\ssh\administrators_authorized_keys" / inheritance:r / grant "Administrators:F" / grant "SYSTEM:F"
        // else path = @"C:\\Users\\" + username + "\\.ssh";
        // icacls.exe path / inheritance:r / grant "username:F" / grant "SYSTEM:F"

        try {
            if (System.IO.File.Exists(path)) {
            } else {
                System.IO.File.Create(path);
            }
            foreach (var key in keys) {
                using StreamWriter sw = System.IO.File.AppendText(path);
                sw.WriteLine((string)key);
            }

        } catch {
            Serilog.Log.Error("Encountered error while adding ssh keys for account:" + username);
        }
    }
}