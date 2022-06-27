using System;

public class Ignition {
    public string Version { get; set; }
}

public class User {
    public string Name { get; set; }
    public string PasswordHash { get; set; }
}

public class Passwd {
    public List<User> Users { get; set; }
}

public class IgnitionFile {
    public Ignition Ignition { get; set; }
    public Passwd Passwd { get; set; }
}
