using TSISP003_Net.SignControllerDataStore.Entities;

namespace TSISP003_Net.SignControllerDataStore;

public interface ISignControllerDataStore
{
    public bool SignControllerExists(string signControllerName);
    public bool AddSignController(string signControllerName);
    public SignController GetSignController(string signControllerName);
}
