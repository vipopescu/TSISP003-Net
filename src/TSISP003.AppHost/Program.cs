var builder = DistributedApplication.CreateBuilder(args);

// TSI-SP-003 sign controller simulator: a single process hosting three sign
// controllers, each on its own raw-TCP port (configured in the simulator's
// appsettings.json), each a group of three text signs. Ports avoid 5000 because
// macOS reserves it for the AirPlay Receiver.
var simulator = builder.AddProject<Projects.TSISP003_Simulator>("simulator")
    .WithEndpoint(name: "tcp-1", scheme: "tcp", port: 5050, targetPort: 5050, isProxied: false)
    .WithEndpoint(name: "tcp-2", scheme: "tcp", port: 5051, targetPort: 5051, isProxied: false)
    .WithEndpoint(name: "tcp-3", scheme: "tcp", port: 5052, targetPort: 5052, isProxied: false);

// API connects to each simulated controller as a separate device. The simulator
// accepts any password, so seed/password offsets only need to be well-formed hex.
builder.AddProject<Projects.TSISP003_Api>("tsisp003-api")
    .WaitFor(simulator)
    .WithEnvironment("SignControllerServices__Devices__sim-1__IpAddress", "127.0.0.1")
    .WithEnvironment("SignControllerServices__Devices__sim-1__Port", "5050")
    .WithEnvironment("SignControllerServices__Devices__sim-1__Address", "01")
    .WithEnvironment("SignControllerServices__Devices__sim-1__SeedOffset", "00")
    .WithEnvironment("SignControllerServices__Devices__sim-1__PasswordOffset", "00")
    .WithEnvironment("SignControllerServices__Devices__sim-2__IpAddress", "127.0.0.1")
    .WithEnvironment("SignControllerServices__Devices__sim-2__Port", "5051")
    .WithEnvironment("SignControllerServices__Devices__sim-2__Address", "02")
    .WithEnvironment("SignControllerServices__Devices__sim-2__SeedOffset", "00")
    .WithEnvironment("SignControllerServices__Devices__sim-2__PasswordOffset", "00")
    .WithEnvironment("SignControllerServices__Devices__sim-3__IpAddress", "127.0.0.1")
    .WithEnvironment("SignControllerServices__Devices__sim-3__Port", "5052")
    .WithEnvironment("SignControllerServices__Devices__sim-3__Address", "03")
    .WithEnvironment("SignControllerServices__Devices__sim-3__SeedOffset", "00")
    .WithEnvironment("SignControllerServices__Devices__sim-3__PasswordOffset", "00");

builder.Build().Run();
