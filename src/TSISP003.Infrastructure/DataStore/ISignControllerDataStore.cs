using TSISP003.Domain.Entities;

namespace TSISP003.Infrastructure.DataStore;

public interface ISignControllerDataStore
{
    bool SignControllerExists(string signControllerName);
    bool AddSignController(string signControllerName);
    SignController GetSignController(string signControllerName);
}
