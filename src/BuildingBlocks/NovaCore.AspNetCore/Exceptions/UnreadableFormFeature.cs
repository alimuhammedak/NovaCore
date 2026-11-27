using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NovaCore.AspNetCore.Exceptions;

/// <summary>
/// The request's <see cref="IFormFeature"/>, reporting a body the form reader can't read (a key or value over
/// <c>FormOptions</c>' limits, too many values, multipart without a boundary: its <see cref="InvalidDataException"/>) as
/// the framework's client-error signal, a <see cref="BadHttpRequestException"/> with 400. Minimal API form binding does
/// the same where it reads the form (<c>RequestDelegateFactory</c>); this covers every other reader, OpenIddict's request
/// extraction and a handler's own <c>ReadFormAsync</c> included. An <see cref="InvalidDataException"/> from anything else
/// (Zip, GZip, Brotli, Tar, a module's own parser) stays what it is, a server error (docs/plan/RESEARCH.md R-156, V6e-3).
/// </summary>
/// <remarks>
/// It wraps the feature the request already has, so the endpoint's form options (<c>IFormOptionsMetadata</c>, which
/// <c>FormFeature</c> reads when it reads the form) and the cached form still apply. It replaces nothing else: a handler
/// that streams a multipart body itself never reads through it.
/// </remarks>
internal sealed class UnreadableFormFeature(IFormFeature inner) : IFormFeature
{
    /// <summary>
    /// The middleware <c>UseCoreWeb</c> puts before authentication (where OpenIddict reads the form): a request with a form
    /// content type gets this feature over its own.
    /// </summary>
    public static Task Install(HttpContext context, RequestDelegate next)
    {
        if (context.Request.HasFormContentType && context.Features.Get<IFormFeature>() is { } feature and not UnreadableFormFeature)
            context.Features.Set<IFormFeature>(new UnreadableFormFeature(feature));

        return next(context);
    }

    public bool HasFormContentType => inner.HasFormContentType;

    public IFormCollection? Form
    {
        get => inner.Form;
        set => inner.Form = value;
    }

    public IFormCollection ReadForm()
    {
        try
        {
            return inner.ReadForm();
        }
        catch (InvalidDataException exception)
        {
            throw Unreadable(exception);
        }
    }

    public async Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await inner.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException exception)
        {
            throw Unreadable(exception);
        }
    }

    private static BadHttpRequestException Unreadable(InvalidDataException exception) =>
        new("The request body can't be read as a form.", StatusCodes.Status400BadRequest, exception);
}
