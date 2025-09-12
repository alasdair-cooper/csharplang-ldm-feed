using AlasdairCooper.CSharpLangLdmFeed.ServiceDefaults;
using Azure;
using Azure.Data.Tables;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using Octokit.Webhooks.Events;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureTableServiceClient("tables");

builder.Services.AddOpenApi();

builder.Services.AddScoped<WebhookEventProcessor, PushWebhookEventProcessor>();

builder.WebHost.ConfigureKestrel(static x => x.AddServerHeader = false);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(static x => x.Servers = []);
}

app.UseHttpsRedirection();

app.MapGet(
    "/feed",
    static async (TableServiceClient tableServiceClient, CancellationToken cancellationToken) =>
    {
        var tableClient = tableServiceClient.GetTableClient("meetings");
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var meetings =
            await tableClient.QueryAsync<LdmEntity>(static x => x.PartitionKey == "meeting", cancellationToken: cancellationToken)
                .OrderByDescending(static x => x.Timestamp)
                .ToListAsync(cancellationToken);

        return Results.Ok(meetings.Select(static x => x.FileName));
    });

app.MapGitHubWebhooks("/github");

app.Run();

internal sealed class PushWebhookEventProcessor(
    TableServiceClient tableServiceClient,
    TimeProvider timeProvider,
    ILogger<PushWebhookEventProcessor> logger) : WebhookEventProcessor
{
    protected override async ValueTask ProcessPushWebhookAsync(
        WebhookHeaders headers,
        PushEvent pushEvent,
        CancellationToken cancellationToken = default)
    {
        var currentYear = timeProvider.GetUtcNow().Year;
        var addedMeetings = pushEvent.Commits.SelectMany(static x => x.Added).Where(x => x.StartsWith($"meetings/{currentYear}"));

        var tableClient = tableServiceClient.GetTableClient("meetings");
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        foreach (var fileName in addedMeetings)
        {
            await tableClient.AddEntityAsync(LdmEntity.New(fileName), cancellationToken);
            logger.LogInformation("Added new LDM: {ldmFileName}", fileName);
        }
    }
}

internal class LdmEntity : ITableEntity
{
    public required string PartitionKey { get; set; }

    public required string RowKey { get; set; }

    public required string FileName { get; init; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public static LdmEntity New(string fileName) =>
        new() { PartitionKey = "meeting", RowKey = Guid.CreateVersion7().ToString(), FileName = fileName };
}