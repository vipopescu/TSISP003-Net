var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.TSISP003_Api>("tsisp003-api");

builder.Build().Run();
