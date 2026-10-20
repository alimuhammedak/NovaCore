using System.Collections.Concurrent;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.Api.Tests;

/// <summary>
/// A Data Protection key store in memory, standing in for the persistent one a host outside Development must configure
/// for the identity provider (a volume, a database, a blob store; PLAN F4c.6).
/// </summary>
public sealed class InMemoryKeyStore : IXmlRepository
{
    private readonly ConcurrentQueue<XElement> _elements = new();

    public IReadOnlyCollection<XElement> GetAllElements() => [.. _elements.Select(element => new XElement(element))];

    public void StoreElement(XElement element, string friendlyName) => _elements.Enqueue(new XElement(element));

    /// <summary>Configures <paramref name="services"/> to keep the Data Protection keys in a new store of this kind.</summary>
    public static IServiceCollection AddTo(IServiceCollection services) =>
        services.Configure<KeyManagementOptions>(options => options.XmlRepository = new InMemoryKeyStore());
}
