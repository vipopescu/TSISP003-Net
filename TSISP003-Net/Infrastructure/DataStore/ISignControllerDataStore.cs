using TSISP003.Domain.Entities;

namespace TSISP003.Infrastructure.DataStore;

public interface ISignControllerDataStore
{
    public bool SignControllerExists(string signControllerName);
    public bool AddSignController(string signControllerName);
    public SignController GetSignController(string signControllerName);
}
