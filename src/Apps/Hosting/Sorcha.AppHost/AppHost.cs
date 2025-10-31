var builder = DistributedApplication.CreateBuilder(args);

var blueprintEngine = builder.AddProject<Projects.Sorcha_Blueprint_Engine>("blueprint-engine")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Sorcha_Blueprint_Designer>("blueprint-designer")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(blueprintEngine)
    .WaitFor(blueprintEngine);

builder.Build().Run();
