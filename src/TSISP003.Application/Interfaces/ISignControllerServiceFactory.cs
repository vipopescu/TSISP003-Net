namespace TSISP003.Application.Interfaces;

public interface ISignControllerServiceFactory
{
    ISignControllerService GetSignControllerService(string deviceName);
    bool ContainsSignController(string deviceName);
}
