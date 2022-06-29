using System.ServiceProcess;

namespace Acetylene.Ignition; 

public interface IServiceController {
    IServiceController[] GetServices();
    void Start();
    void Stop();
    
    string ServiceName { get; set; }
}