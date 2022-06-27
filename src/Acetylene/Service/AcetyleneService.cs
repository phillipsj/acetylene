namespace Acetylene.Service; 

using System.Timers;

public class AcetyleneService {
    private readonly Timer _timer;

    public AcetyleneService() {
        _timer = new Timer(1000) {AutoReset = true};
        _timer.Elapsed += (sender, eventArgs) => Console.WriteLine("It is {0} and all is well", DateTime.Now);
    }

    public void Start() { _timer.Start(); }

    public void Stop() { _timer.Stop(); }
}