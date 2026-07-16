using System.Globalization;

namespace SengokuScroll.Localization.Abstractions;

/// <summary>运行时文化上下文（线程/请求级）。</summary>
public interface ICultureContext
{
    CultureInfo Current { get; }

    void SetCulture(string cultureName);
}
