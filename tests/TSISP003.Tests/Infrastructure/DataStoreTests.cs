using TSISP003.Domain.Entities;
using TSISP003.Infrastructure.DataStore;

namespace TSISP003.Tests.Infrastructure;

public class DataStoreTests
{
    #region Interface Tests

    [Fact]
    public void SignControllerDataStore_ImplementsInterface()
    {
        // Arrange & Act
        var dataStore = new SignControllerDataStore();

        // Assert
        Assert.IsAssignableFrom<ISignControllerDataStore>(dataStore);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void SignControllerDataStore_CanBeInstantiated()
    {
        // Act
        var dataStore = new SignControllerDataStore();

        // Assert
        Assert.NotNull(dataStore);
    }

    #endregion

    #region SignControllerExists Tests

    [Fact]
    public void SignControllerExists_ThrowsNotImplementedException()
    {
        // Arrange
        var dataStore = new SignControllerDataStore();

        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            dataStore.SignControllerExists("controller1"));
    }

    #endregion

    #region AddSignController Tests

    [Fact]
    public void AddSignController_ThrowsNotImplementedException()
    {
        // Arrange
        var dataStore = new SignControllerDataStore();

        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            dataStore.AddSignController("controller1"));
    }

    #endregion

    #region GetSignController Tests

    [Fact]
    public void GetSignController_ThrowsNotImplementedException()
    {
        // Arrange
        var dataStore = new SignControllerDataStore();

        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            dataStore.GetSignController("controller1"));
    }

    #endregion

    #region Interface Method Signatures

    [Fact]
    public void ISignControllerDataStore_HasSignControllerExistsMethod()
    {
        // Assert - verifies interface has the method
        var interfaceType = typeof(ISignControllerDataStore);
        var method = interfaceType.GetMethod("SignControllerExists");

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(string), method.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void ISignControllerDataStore_HasAddSignControllerMethod()
    {
        // Assert - verifies interface has the method
        var interfaceType = typeof(ISignControllerDataStore);
        var method = interfaceType.GetMethod("AddSignController");

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(string), method.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void ISignControllerDataStore_HasGetSignControllerMethod()
    {
        // Assert - verifies interface has the method
        var interfaceType = typeof(ISignControllerDataStore);
        var method = interfaceType.GetMethod("GetSignController");

        Assert.NotNull(method);
        Assert.Equal(typeof(SignController), method.ReturnType);
        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(string), method.GetParameters()[0].ParameterType);
    }

    #endregion
}
