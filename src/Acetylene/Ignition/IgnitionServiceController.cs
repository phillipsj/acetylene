using System.ServiceProcess;

namespace Acetylene.Ignition; 

public class IgnitionServiceController : IServiceController {
    public IServiceController[] GetServices() {
        return ServiceController.GetServices().Select(x => new WindowsServiceController(x))
            .Cast<IServiceController>().ToArray();
    }

    public void Start() {
        throw new NotImplementedException();
    }

    public void Stop() {
        throw new NotImplementedException();
    }

    public string ServiceName {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}