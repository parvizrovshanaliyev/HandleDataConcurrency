using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HandleDataConcurrency.Api;
using HandleDataConcurrency.Api.Domain.Documents;
using HandleDataConcurrency.ApiTests.Extensions;
using Microsoft.AspNetCore.TestHost;

namespace HandleDataConcurrency.ApiTests.Controllers;

public class DocumentControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public DocumentControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test method for concurrently obtaining document numbers and testing Concurrency Conflicts handling.
    /// </summary>
    [Fact]
    public async Task GivenDocumentEndpoint_WhenConcurrentDocumentCreation_ThenOk()
    {
        using var client1 = _factory.CreateClient();
        using var client2 = _factory.CreateClient();

        // Use Task.WhenAll to run multiple tasks concurrently
        var tasks = new List<Task<HttpResponseMessage>>
        {
            client1.PostAsync("api/document/create-document", new StringContent(string.Empty)),
            client2.PostAsync("api/document/create-document", new StringContent(string.Empty))
        };

        var responses = await Task.WhenAll(tasks);

        responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
        responses[1].StatusCode.Should().Be(HttpStatusCode.OK);

        var document1 = await responses[0].ReadAndAssertSuccessAsync<Document>();
        var document2 = await responses[1].ReadAndAssertSuccessAsync<Document>();

        document1.Number.Should().NotBe(document2.Number);
    }
}