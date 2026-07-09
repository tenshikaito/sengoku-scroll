namespace SengokuScroll.Common.Extensions;

public static class StringExtensions
{
    public static string Format(this string s, params object[] args) => string.Format(s, args);
}
