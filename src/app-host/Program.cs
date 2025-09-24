var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker")
    .ConfigureComposeFile(
        static x =>
        {
            foreach (var (_, value) in x.Networks.Where(static x => x.Key == "aspire"))
            {
                value.External = true;
            }
        });

var gitHubApiTokenParameter = builder.AddParameter("github-api-token", true);

builder.AddProject<Projects.AlasdairCooper_CSharpLangLdmFeed_Api>("api")
    .WithEnvironment("GITHUB_API_TOKEN", gitHubApiTokenParameter)
    .WithExternalHttpEndpoints();

builder.Build().Run();