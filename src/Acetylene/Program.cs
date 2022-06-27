using Acetylene.Service;
using Serilog;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Topshelf;

namespace Acetylene; 

public class Program {
    [SupportedOSPlatform("windows")]
    public static void SupportedOnWindows() { }
    public static void Main() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            SupportedOnWindows();
        }
        var ts = HostFactory.Run(x => {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
            x.UseSerilog();

            x.Service<AcetyleneService>(s => {
                s.ConstructUsing(name => new AcetyleneService());
                s.WhenStarted(a => a.Start());
                s.WhenStopped(a => a.Stop());
            });
            
            // Come back to this
            x.SetDescription("Ignition for Windows!");
            x.SetDisplayName("Acetylene");
            x.SetServiceName("Acetylene");
            
            x.RunAsLocalSystem();
            
            x.SetStartTimeout(TimeSpan.FromSeconds(30));
            x.SetStopTimeout(TimeSpan.FromSeconds(30));

            x.EnableServiceRecovery(r =>
            {
                r.RestartService(3);
                r.SetResetPeriod(2);
            });
            

            x.OnException((exception) =>
            {
                Console.WriteLine("Exception thrown - " + exception.Message);
            });
        });
        var exitCode = (int)Convert.ChangeType(ts, ts.GetTypeCode());
        Environment.ExitCode = exitCode;
    }
}