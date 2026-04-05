using System.Net;
using System.Text;

namespace HytaleAdmin.Tests;

/// <summary>
/// A mock HttpMessageHandler that returns canned responses based on URL patterns.
/// Creates fresh HttpResponseMessage instances per request to avoid stream reuse issues.
/// </summary>
public class MockHttpHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> match, string jsonBody, HttpStatusCode status)> _rules = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>All captured requests, in order.</summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    /// <summary>Register a response for requests where the URL contains the given path.</summary>
    public MockHttpHandler Respond(string urlContains, string jsonBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        _rules.Add((req => req.RequestUri?.ToString().Contains(urlContains) == true, jsonBody, status));
        return this;
    }

    /// <summary>Register a response for requests matching a predicate.</summary>
    public MockHttpHandler Respond(Func<HttpRequestMessage, bool> match, string jsonBody,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        _rules.Add((match, jsonBody, status));
        return this;
    }

    /// <summary>Register a failing response for requests where the URL contains the given path.</summary>
    public MockHttpHandler RespondError(string urlContains, HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        _rules.Add((req => req.RequestUri?.ToString().Contains(urlContains) == true,
            "{\"error\":\"mock error\"}", status));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        foreach (var (match, jsonBody, status) in _rules)
        {
            if (match(request))
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":\"no mock registered\"}", Encoding.UTF8, "application/json")
        });
    }
}
