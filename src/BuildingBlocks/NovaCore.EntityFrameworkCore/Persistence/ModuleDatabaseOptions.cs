using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace NovaCore.EntityFrameworkCore.Persistence;

/// <summary>The host's database for every module DbContext, set by <c>AddModuleDatabase</c>.</summary>
internal sealed class ModuleDatabaseOptions
{
    /// <summary>Configures the provider, the connection and the provider's options of a module context.</summary>
    [Required(ErrorMessage = "No database for the module DbContexts: the host calls AddModuleDatabase with their provider and connection.")]
    public Action<IServiceProvider, DbContextOptionsBuilder>? Configure { get; set; }
}

[OptionsValidator]
internal sealed partial class ModuleDatabaseOptionsValidator : IValidateOptions<ModuleDatabaseOptions>;
