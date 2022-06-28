using System.IO.Abstractions;

namespace Acetylene;

public static class Extensions {
    public static IDriveInfo GetIgnitionDrive(this IDriveInfo[] drives) {
        var ready = drives.Where(x => x.IsReady);
        var ignition = drives.Where(x => x.VolumeLabel == "ignition").ToList();
        return ignition.Single();
    }

    public static IDriveInfo GetCombustionDrive(this IDriveInfo[] drives) {
        var ready = drives.Where(x => x.IsReady);
        var combustion = drives.Where(x => x.VolumeLabel == "combustion").ToList();
        return !combustion.Any() ? drives.GetIgnitionDrive() : combustion.Single();
    }
}