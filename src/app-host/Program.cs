var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AlasdairCooper_CSharpLangLdmFeed_Api>("api");

builder.Build().Run();
