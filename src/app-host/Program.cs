var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.AlasdairCooper_CSharpLangLdmFeed_Api>("api");

var storage = builder.AddAzureStorage("storage").RunAsEmulator().AddTables("tables");

api.WithReference(storage).WaitFor(storage);

builder.Build().Run();
