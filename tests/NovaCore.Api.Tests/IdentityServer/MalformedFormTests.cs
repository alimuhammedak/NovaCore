using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The provider's form endpoints given bodies a browser never sends (F6e.2's Schemathesis run, S-14): the sign-in pages'
/// forms, and the protocol endpoints whose form OpenIddict reads before the handler. A body the framework can't read as a
/// form is the client's error, a 400 without the reader's message (Y-35); one it can read is answered by the endpoint's
/// own rules. Neither is a 500.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class MalformedFormTests(IdentityServerFixture fixture) : IClassFixture<IdentityServerFixture>
{
    private const string UrlEncoded = "application/x-www-form-urlencoded";

    private static readonly string[] FormEndpoints =
    [
        "/account/login", "/account/login/2fa", "/account/forgot-password", "/account/reset-password",
        "/connect/revoke", "/connect/token", "/connect/authorize", "/connect/endsession"
    ];

    // Bodies the form reader refuses (FormOptions' limits, a NUL character, multipart without a boundary).
    private static readonly string[] UnreadableBodies = ["long-key", "long-value", "too-many-values", "nul-character", "multipart-without-boundary"];

    // Bodies it reads, which the endpoint then refuses or ignores.
    private static readonly string[] ReadableBodies = ["empty", "missing-fields", "json", "bad-percent-encoding", "invalid-utf8", "unknown-charset", "repeated-fields"];

    private readonly IdentityServerTestHost _host = fixture.Host;

    public static TheoryData<string, string> Unreadable() => Combine(UnreadableBodies);

    public static TheoryData<string, string> Readable() => Combine(ReadableBodies);

    [Theory]
    [MemberData(nameof(Unreadable))]
    public async Task A_body_that_is_not_a_readable_form_is_a_bad_request(string path, string body)
    {
        var response = await PostAsync(path, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("limit", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", text, StringComparison.OrdinalIgnoreCase);

        // OpenIddict refuses a multipart body itself, with its own error; every other one is the core's ProblemDetails.
        if (string.Equals(response.Content.Headers.ContentType?.MediaType, "application/problem+json", StringComparison.Ordinal))
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Bad Request", problem.GetProperty("title").GetString());
            Assert.False(problem.TryGetProperty("detail", out _));
        }
        else
        {
            Assert.StartsWith("/connect/", path, StringComparison.Ordinal);
            Assert.Equal("multipart-without-boundary", body);
        }
    }

    [Theory]
    [MemberData(nameof(Readable))]
    public async Task A_readable_form_with_wrong_fields_is_never_a_server_error(string path, string body)
    {
        var response = await PostAsync(path, body);

        Assert.InRange((int)response.StatusCode, 200, 499);
    }

    private static TheoryData<string, string> Combine(string[] bodies)
    {
        var data = new TheoryData<string, string>();
        foreach (var path in FormEndpoints)
        {
            foreach (var body in bodies)
                data.Add(path, body);
        }

        return data;
    }

    private async Task<HttpResponseMessage> PostAsync(string path, string body)
    {
        using var client = _host.CreateClient();
        using var content = Body(body);
        return await client.PostAsync(new Uri(path, UriKind.Relative), content);
    }

    private static ByteArrayContent Body(string kind) => kind switch
    {
        "long-key" => Text(new string('k', 5_000) + "=v"),
        "long-value" => Text("token=" + new string('v', 5 * 1024 * 1024)),
        "too-many-values" => Text(string.Join('&', Enumerable.Range(0, 1_100).Select(i => $"k{i}=v"))),
        "nul-character" => Text("token=%00&Email=%00&ReturnUrl=%00"),
        "multipart-without-boundary" => Text("x", "multipart/form-data"),
        "empty" => Text(""),
        "missing-fields" => Text("Email=a"),
        "json" => Text("{\"token\":1}", "application/json"),
        "bad-percent-encoding" => Text("Email=%zz%&Password=%E0%A4%A&token=%%"),
        "invalid-utf8" => Bytes([.. "Email="u8, 0xC3, 0x28, 0xFF, 0xFE, .. "&token="u8, 0xF0, 0x28, 0x8C, 0x28], UrlEncoded),
        "unknown-charset" => Text("Email=a", UrlEncoded + "; charset=no-such-charset"),
        "repeated-fields" => Text("token=a&token=b&client_id=x&client_id=y&Email=a&Email=b"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static ByteArrayContent Text(string text, string type = UrlEncoded) => Bytes(Encoding.ASCII.GetBytes(text), type);

    private static ByteArrayContent Bytes(byte[] bytes, string type)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(type);
        return content;
    }
}
