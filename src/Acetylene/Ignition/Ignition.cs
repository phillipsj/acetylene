public class Ignition {
    public string?    Version { get; set; }
}

public class User {
    public string?   Name { get; set; }
    public string?  PasswordHash { get; set; }
    public string?   PrimaryGroup { get; set; }  
    public List<string>?   Groups { get; set; }
    public List<string>?  SshAuthorizedKeys { get; set; }
}

public class Passwd {
    public List<User>?  Users { get; set; }
}

public class Storage { 
    public List<File>?  Files { get; set; }
}

public class File {
    public string?  Path { get; set; }
    public string?  Mode { get; set; }
    public Contents?  Contents { get; set; }
    public bool Overwrite { get; set; }
}

public class Contents {
    public string? Source { get; set; }
}

public class Systemd {
    public List<Unit>?  Units { get; set; }
}

public class Unit {
    public string?  Name { get; set; }
    public bool  Enabled { get; set; }
    public string?  Contents { get; set; }
}
public class IgnitionFile {
    public Ignition?  Ignition { get; set; }
    public Passwd?  Passwd { get; set; }
    public Storage?  Storage { get; set; }
    public Systemd?  Systemd { get; set; }
}
