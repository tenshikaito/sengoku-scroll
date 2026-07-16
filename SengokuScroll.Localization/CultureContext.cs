using System.Globalization;
using SengokuScroll.Localization.Abstractions;

namespace SengokuScroll.Localization;

/// <summary>默认可变文化上下文。</summary>
public sealed class CultureContext : ICultureContext
{
    private CultureInfo _current = CultureInfo.GetCultureInfo("zh-CN");

    public CultureInfo Current => _current;

    public void SetCulture(string cultureName)
        => _current = CultureInfo.GetCultureInfo(cultureName);
}
