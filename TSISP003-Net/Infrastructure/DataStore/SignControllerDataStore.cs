using System.Collections.Concurrent;
using TSISP003.Domain.Entities;

namespace TSISP003.Infrastructure.DataStore;

public class SignControllerDataStore : ISignControllerDataStore
{
    ConcurrentDictionary<string,SignController> signControllers = new ConcurrentDictionary<string,SignController>();

    public bool AddSignController(string signControllerName)
    {
        throw new NotImplementedException();
    }

    public SignController GetSignController(string signControllerName)
    {
        throw new NotImplementedException();
    }

    public bool SignControllerExists(string signControllerName)
    {
        throw new NotImplementedException();
    }
}
