using Microsoft.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Refuses a password from a list of commonly used ones (NIST SP 800-63B-4), whatever its length, ignoring case. The list
/// ships in the package (<c>CommonPasswords.txt</c>, from SecLists, MIT): no call to an outside service sees a password.
/// </summary>
/// <remarks>
/// A singleton, so the list is read once; Identity's own <c>AddPasswordValidator</c> would register it per scope.
/// </remarks>
internal sealed class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private const string ResourceName = "NovaCore.Identity.OpenIddict.CommonPasswords.txt";

    private readonly HashSet<string> _passwords = Load();

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password) =>
        Task.FromResult(password is not null && _passwords.Contains(password)
            ? IdentityResult.Failed(new IdentityError
            {
                Code = "CommonPassword",
                Description = "This password is too common. Choose another one."
            })
            : IdentityResult.Success);

    private static HashSet<string> Load()
    {
        using var stream = typeof(CommonPasswordValidator).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);

        var passwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is { } line)
            if (line.Length > 0 && !line.StartsWith('#'))
                passwords.Add(line);

        return passwords;
    }
}
