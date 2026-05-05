using ProjectHub.Persistence;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ProjectHub.Tests;

public sealed class JsonClaudeDataProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;
    private readonly string _workingDir;

    public JsonClaudeDataProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ClaudeDataProviderTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "store.json");
        _workingDir = Path.Combine(_tempDir, "wd");
        Directory.CreateDirectory(_workingDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private JsonClaudeDataProvider CreateProvider() => new(
        Options.Create(new JsonDataProviderOptions { FilePath = _filePath }),
        new TestHostEnvironment(_tempDir),
        NullLogger<JsonClaudeDataProvider>.Instance);

    private static async Task<ClaudeProject> CreateProjectAsync(JsonClaudeDataProvider provider, string name, string workingDirectory)
    {
        var client = await provider.CreateClientAsync($"client-{name}-{Guid.NewGuid():N}");
        return await provider.CreateProjectAsync(name, workingDirectory, client.Id);
    }

    [Fact]
    public async Task CreateProject_AssignsGuidAndPersists()
    {
        var provider = CreateProvider();

        var project = await CreateProjectAsync(provider, "planning", _workingDir);

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("planning", project.Name);
        Assert.Equal(_workingDir, project.WorkingDirectory);
        Assert.NotEqual(Guid.Empty, project.ClientId);
        Assert.True(File.Exists(_filePath));

        var fetched = await provider.GetProjectAsync(project.Id);
        Assert.NotNull(fetched);
        Assert.Equal(project.Id, fetched!.Id);
        Assert.Equal(_workingDir, fetched.WorkingDirectory);
        Assert.Equal(project.ClientId, fetched.ClientId);
    }

    [Fact]
    public async Task CreateProject_WithBlankName_Throws()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateProjectAsync("   ", _workingDir, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateProject_WithBlankWorkingDirectory_Throws()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateProjectAsync("name", "   ", Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateProject_WithUnknownClient_Throws()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreateProjectAsync("name", _workingDir, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetProject_ForUnknownId_ReturnsNull()
    {
        var provider = CreateProvider();

        var fetched = await provider.GetProjectAsync(Guid.NewGuid());

        Assert.Null(fetched);
    }

    [Fact]
    public async Task ListProjects_ReturnsProjectsInCreationOrder()
    {
        var provider = CreateProvider();

        var first = await CreateProjectAsync(provider, "first", _workingDir);
        await Task.Delay(5);
        var second = await CreateProjectAsync(provider, "second", _workingDir);
        await Task.Delay(5);
        var third = await CreateProjectAsync(provider, "third", _workingDir);

        var projects = await provider.ListProjectsAsync();

        Assert.Equal(new[] { first.Id, second.Id, third.Id }, projects.Select(t => t.Id));
    }

    [Fact]
    public async Task CreateJob_ForUnknownProject_Throws()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreateJobAsync(Guid.NewGuid(), "hello"));
    }

    [Fact]
    public async Task CreateJob_StartsInQueuedStatusAndPersists()
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);

        var job = await provider.CreateJobAsync(project.Id, "hello");

        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal(project.Id, job.ProjectId);
        Assert.Equal("hello", job.Message);
        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Null(job.Response);

        var fetched = await provider.GetJobAsync(job.Id);
        Assert.NotNull(fetched);
        Assert.Equal(job.Id, fetched!.Id);
    }

    [Fact]
    public async Task UpdateJob_PersistsAllFields()
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);
        var job = await provider.CreateJobAsync(project.Id, "hi");

        job.Status = JobStatus.Completed;
        job.Response = "hello back";
        job.ExitCode = 0;
        job.DurationMs = 1234;
        job.StartedAt = DateTimeOffset.UtcNow;
        job.CompletedAt = DateTimeOffset.UtcNow;

        await provider.UpdateJobAsync(job);

        var fetched = await provider.GetJobAsync(job.Id);
        Assert.NotNull(fetched);
        Assert.Equal(JobStatus.Completed, fetched!.Status);
        Assert.Equal("hello back", fetched.Response);
        Assert.Equal(0, fetched.ExitCode);
        Assert.Equal(1234, fetched.DurationMs);
        Assert.NotNull(fetched.CompletedAt);
    }

    [Fact]
    public async Task UpdateJob_ForUnknownId_Throws()
    {
        var provider = CreateProvider();

        var stranger = new ClaudeJob
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Message = "x",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.UpdateJobAsync(stranger));
    }

    [Fact]
    public async Task ListJobsByProject_ReturnsOnlyMatchingProjectOrderedByCreatedAt()
    {
        var provider = CreateProvider();
        var alpha = await CreateProjectAsync(provider, "alpha", _workingDir);
        var beta = await CreateProjectAsync(provider, "beta", _workingDir);

        var a1 = await provider.CreateJobAsync(alpha.Id, "a1");
        await Task.Delay(5);
        var b1 = await provider.CreateJobAsync(beta.Id, "b1");
        await Task.Delay(5);
        var a2 = await provider.CreateJobAsync(alpha.Id, "a2");

        var alphaJobs = await provider.ListJobsByProjectAsync(alpha.Id);

        Assert.Equal(new[] { a1.Id, a2.Id }, alphaJobs.Select(j => j.Id));
        Assert.DoesNotContain(b1.Id, alphaJobs.Select(j => j.Id));
    }

    [Fact]
    public async Task ListJobsByStatus_ReturnsOnlyMatching()
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);
        var queued = await provider.CreateJobAsync(project.Id, "queued");
        var inProgress = await provider.CreateJobAsync(project.Id, "running");

        inProgress.Status = JobStatus.Processing;
        await provider.UpdateJobAsync(inProgress);

        var stillQueued = await provider.ListJobsByStatusAsync(JobStatus.Queued);

        Assert.Single(stillQueued);
        Assert.Equal(queued.Id, stillQueued[0].Id);
    }

    [Fact]
    public async Task RoundTrip_AcrossProviderInstances_ReadsPersistedData()
    {
        var first = CreateProvider();
        var project = await CreateProjectAsync(first, "persistence", _workingDir);
        var job = await first.CreateJobAsync(project.Id, "round-trip");

        job.Status = JobStatus.Completed;
        job.Response = "ok";
        job.ExitCode = 0;
        job.DurationMs = 50;
        job.CompletedAt = DateTimeOffset.UtcNow;
        await first.UpdateJobAsync(job);

        var second = CreateProvider();

        var projects = await second.ListProjectsAsync();
        Assert.Single(projects);
        Assert.Equal(project.Id, projects[0].Id);
        Assert.Equal(_workingDir, projects[0].WorkingDirectory);

        var fetched = await second.GetJobAsync(job.Id);
        Assert.NotNull(fetched);
        Assert.Equal("ok", fetched!.Response);
        Assert.Equal(JobStatus.Completed, fetched.Status);
    }

    [Fact]
    public async Task GetJob_AfterUpdate_DoesNotShareReferenceWithStore()
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);
        var job = await provider.CreateJobAsync(project.Id, "hi");

        var fetched = await provider.GetJobAsync(job.Id);
        Assert.NotNull(fetched);
        fetched!.Status = JobStatus.Completed;
        fetched.Response = "should not be persisted without UpdateJobAsync";

        var refetched = await provider.GetJobAsync(job.Id);
        Assert.Equal(JobStatus.Queued, refetched!.Status);
        Assert.Null(refetched.Response);
    }

    [Fact]
    public async Task GetProject_DoesNotShareReferenceWithStore()
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);

        var fetched = await provider.GetProjectAsync(project.Id);
        Assert.NotNull(fetched);

        Assert.NotSame(project, fetched);
    }

    [Fact]
    public async Task CreateTicket_ForUnknownProject_Throws()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreateTicketAsync(Guid.NewGuid(), "PROJ-1", "Title", "body"));
    }

    [Fact]
    public async Task CreateTicket_PersistsAllFields()
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);

        var ticket = await provider.CreateTicketAsync(project.Id, "PROJ-1", "Hello", "## body\nline");

        Assert.NotEqual(Guid.Empty, ticket.Id);
        Assert.Equal(project.Id, ticket.ProjectId);
        Assert.Equal("PROJ-1", ticket.Code);
        Assert.Equal("Hello", ticket.Title);
        Assert.Equal("## body\nline", ticket.Body);

        var fetched = await provider.GetTicketAsync(ticket.Id);
        Assert.NotNull(fetched);
        Assert.Equal(ticket.Id, fetched!.Id);
        Assert.Equal("PROJ-1", fetched.Code);
    }

    [Fact]
    public async Task CreateTicket_TrimsCodeAndTitle()
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);

        var ticket = await provider.CreateTicketAsync(project.Id, "  PROJ-7  ", "  Hello  ", "body");

        Assert.Equal("PROJ-7", ticket.Code);
        Assert.Equal("Hello", ticket.Title);
    }

    [Theory]
    [InlineData("", "title", "body")]
    [InlineData("   ", "title", "body")]
    [InlineData("code", "", "body")]
    [InlineData("code", "   ", "body")]
    public async Task CreateTicket_WithBlankCodeOrTitle_Throws(string code, string title, string body)
    {
        var provider = CreateProvider();
        var project = await CreateProjectAsync(provider, "t", _workingDir);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.CreateTicketAsync(project.Id, code, title, body));
    }

    [Fact]
    public async Task ListTicketsByProject_ReturnsOnlyMatchingProjectInOrder()
    {
        var provider = CreateProvider();
        var alpha = await CreateProjectAsync(provider, "alpha", _workingDir);
        var beta = await CreateProjectAsync(provider, "beta", _workingDir);

        var a1 = await provider.CreateTicketAsync(alpha.Id, "A-1", "first", "x");
        await Task.Delay(5);
        var b1 = await provider.CreateTicketAsync(beta.Id, "B-1", "other project", "x");
        await Task.Delay(5);
        var a2 = await provider.CreateTicketAsync(alpha.Id, "A-2", "second", "x");

        var alphaTickets = await provider.ListTicketsByProjectAsync(alpha.Id);

        Assert.Equal(new[] { a1.Id, a2.Id }, alphaTickets.Select(t => t.Id));
        Assert.DoesNotContain(b1.Id, alphaTickets.Select(t => t.Id));
    }

    [Fact]
    public async Task RoundTrip_PersistsTickets()
    {
        var first = CreateProvider();
        var project = await CreateProjectAsync(first, "p", _workingDir);
        var ticket = await first.CreateTicketAsync(project.Id, "T-1", "title", "body");

        var second = CreateProvider();

        var fetched = await second.GetTicketAsync(ticket.Id);

        Assert.NotNull(fetched);
        Assert.Equal("T-1", fetched!.Code);
        Assert.Equal(project.Id, fetched.ProjectId);
    }

    private sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "ProjectHub.Tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.PhysicalFileProvider(contentRoot);
    }
}
