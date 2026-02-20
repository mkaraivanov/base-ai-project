using System.Globalization;
using Application.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Tests.Unit.Helpers;

/// <summary>
/// Creates real <see cref="IStringLocalizer{SharedResource}"/> instances backed by
/// the production .resx files so validator / service tests can assert localised messages.
/// </summary>
public static class LocalizerHelper
{
    private static readonly IStringLocalizerFactory _factory;

    static LocalizerHelper()
    {
        // Build a minimal DI container – no ResourcesPath so the factory resolves
        // embedded resources by their fully-qualified type name, which matches the
        // manifest resource names compiled into the Application assembly.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        _factory = services.BuildServiceProvider()
                           .GetRequiredService<IStringLocalizerFactory>();
    }

    /// <summary>
    /// Returns a localizer that resolves every string using the current thread's
    /// <see cref="CultureInfo.CurrentUICulture"/> (typically <c>en-US</c> in test runners).
    /// Because the English text <em>is</em> the resource key, the key is returned as-is
    /// when no matching resource file is found, giving the expected English string.
    /// Use this for tests that only care about property names, not message text.
    /// </summary>
    public static IStringLocalizer<SharedResource> CreateDefault() =>
        new DefaultLocalizer(_factory.Create(typeof(SharedResource)));

    /// <summary>
    /// Returns a localizer that resolves every string with the <c>bg-BG</c> UI culture.
    /// The thread's <see cref="CultureInfo.CurrentUICulture"/> is switched for the
    /// duration of each lookup and restored immediately afterward.
    /// </summary>
    public static IStringLocalizer<SharedResource> CreateBg() =>
        new CultureBoundLocalizer(_factory.Create(typeof(SharedResource)), "bg-BG");

    // ─── inner wrappers ───────────────────────────────────────────────────────

    private sealed class DefaultLocalizer : IStringLocalizer<SharedResource>
    {
        private readonly IStringLocalizer _inner;
        public DefaultLocalizer(IStringLocalizer inner) => _inner = inner;

        public LocalizedString this[string name] => _inner[name];
        public LocalizedString this[string name, params object[] arguments] => _inner[name, arguments];
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _inner.GetAllStrings(includeParentCultures);
    }

    private sealed class CultureBoundLocalizer : IStringLocalizer<SharedResource>
    {
        private readonly IStringLocalizer _inner;
        private readonly CultureInfo _culture;

        public CultureBoundLocalizer(IStringLocalizer inner, string cultureName)
        {
            _inner = inner;
            _culture = new CultureInfo(cultureName);
        }

        public LocalizedString this[string name]
        {
            get
            {
                using var _ = new UICultureScope(_culture);
                return _inner[name];
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                using var _ = new UICultureScope(_culture);
                return _inner[name, arguments];
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            using var _ = new UICultureScope(_culture);
            // Materialise before the scope exits so the culture is still active.
            return _inner.GetAllStrings(includeParentCultures).ToList();
        }
    }

    private readonly struct UICultureScope : IDisposable
    {
        private readonly CultureInfo _saved;

        public UICultureScope(CultureInfo culture)
        {
            _saved = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose() => CultureInfo.CurrentUICulture = _saved;
    }
}
