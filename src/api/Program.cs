using System.ComponentModel.DataAnnotations;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using AlasdairCooper.CSharpLangLdmFeed.ServiceDefaults;
using Humanizer;
using Microsoft.Extensions.Options;
using Octokit;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddScoped(static _ => new GitHubClient(new ProductHeaderValue("csharplang-ldm-feed")));

builder.Services.AddOptions<LdmFeedOptions>()
    .Configure(
        static x =>
        {
            x.DotnetOrgName = "dotnet";
            x.CSharpLangRepoName = "csharplang";
            x.FeedItemsCount = 20;
            x.MeetingsPathTemplate = "meetings/{0}";
            x.LdmHomePage = "https://github.com/dotnet/csharplang/tree/main/meetings";
        })
    .ValidateOnStart()
    .ValidateDataAnnotations();

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
    static async (GitHubClient gitHubClient, TimeProvider timeProvider, IOptionsSnapshot<LdmFeedOptions> options) =>
    {
        var thisYear = timeProvider.GetUtcNow().Year;

        var lastYearsMeetings =
            await gitHubClient.Repository.Content.GetAllContents(
                options.Value.DotnetOrgName,
                options.Value.CSharpLangRepoName,
                string.Format(options.Value.MeetingsPathTemplate, thisYear - 1));

        var thisYearsMeetings =
            await gitHubClient.Repository.Content.GetAllContents(
                options.Value.DotnetOrgName,
                options.Value.CSharpLangRepoName,
                string.Format(options.Value.MeetingsPathTemplate, thisYear));

        var meetings =
            lastYearsMeetings.Concat(thisYearsMeetings)
                .Where(static x => LdmFeedOptions.LdmNotesRegex().IsMatch(x.Name))
                .Select(
                    static x =>
                    {
                        var publishedDate = DateTimeOffset.Parse(LdmFeedOptions.LdmNotesRegex().Match(x.Name).Groups[1].Value);

                        return new SyndicationItem(
                            $"C# Language Design Meeting for {publishedDate:MMMM} {publishedDate.Day.Ordinalize()}, {publishedDate:yyyy}",
                            "",
                            new Uri(x.HtmlUrl)) { Id = x.Name, PublishDate = publishedDate };
                    })
                .OrderByDescending(static x => x.PublishDate)
                .Take(options.Value.FeedItemsCount)
                .ToList();

        var feed = new SyndicationFeed("C# LDMs", "C# Language Design Meeting notes.", new Uri(options.Value.LdmHomePage), meetings);

        var settings =
            new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = true,
                Indent = true,
                Async = true
            };

        using var stream = new MemoryStream();
        await using var xmlWriter = XmlWriter.Create(stream, settings);

        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(xmlWriter);
        xmlWriter.Flush();

        return Results.File(stream.ToArray(), "application/rss+xml; charset=utf-8");
    });

app.Run();

internal partial class LdmFeedOptions
{
    [Range(1, 100)]
    public int FeedItemsCount { get; set; }

    [Required]
    public required string DotnetOrgName { get; set; }

    [Required]
    public required string CSharpLangRepoName { get; set; }

    [Required]
    public required string MeetingsPathTemplate { get; set; }

    [Required]
    public required string LdmHomePage { get; set; }

    [GeneratedRegex("LDM-([0-9]{4}-[0-9]{2}-[0-9]{2}).md")]
    public static partial Regex LdmNotesRegex();
}