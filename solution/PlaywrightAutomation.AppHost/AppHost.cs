var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.PlaywrightAutomation_MAUI>("maui");

builder.AddProject<Projects.PlaywrightAutomation_API>("api");

builder.Build().Run();
