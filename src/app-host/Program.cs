var builder = DistributedApplication.CreateBuilder(args);

var gitHubApiTokenParameter = builder.AddParameter("github-api-token", true);

builder.AddProject<Projects.AlasdairCooper_CSharpLangLdmFeed_Api>("api").WithEnvironment("GITHUB_API_TOKEN", gitHubApiTokenParameter);

builder.Build().Run();