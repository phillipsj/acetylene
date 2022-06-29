using System.ServiceProcess;

namespace Acetylene.Ignition; 

public class WindowsServiceController : IServiceController {
    private readonly ServiceController _service;

    public WindowsServiceController(ServiceController service) {
        _service = service;
    }
    public IServiceController[] GetServices() {
        throw new NotImplementedException();
    }

    public void Start() {
        _service.Start();
    }

    public void Stop() {
        _service.Stop();
    }

    public string ServiceName {
        get => _service.ServiceName; 
        set => _service.ServiceName = value;
    }
}