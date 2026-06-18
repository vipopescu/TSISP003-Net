var builder = DistributedApplication.CreateBuilder(args);

// TSI-SP-003 sign controller simulator: a single device with one text sign,
// listening for raw TCP on a fixed port so the API can connect to it.
var simulator = builder.AddProject<Projects.TSISP003_Simulator>("simulator")
    .WithEnvironment("Simulator__Port", "5050")
    .WithEnvironment("Simulator__Address", "01")
    .WithEnvironment("Simulator__Seed", "18")
    .WithEndpoint(name: "tcp", scheme: "tcp", port: 5050, targetPort: 5050, isProxied: false);

// API talks to the simulator as a configured device. The simulator accepts any
// password, so the seed/password offsets only need to be well-formed hex.
builder.AddProject<Projects.TSISP003_Api>("tsisp003-api")
    .WaitFor(simulator)
    .WithEnvironment("SignControllerServices__Devices__sim__IpAddress", "127.0.0.1")
    .WithEnvironment("SignControllerServices__Devices__sim__Port", "5050")
    .WithEnvironment("SignControllerServices__Devices__sim__Address", "01")
    .WithEnvironment("SignControllerServices__Devices__sim__SeedOffset", "00")
    .WithEnvironment("SignControllerServices__Devices__sim__PasswordOffset", "00");

builder.Build().Run();
