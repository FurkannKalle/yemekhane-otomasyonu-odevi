using System.Globalization;

namespace Yemekhane.Servisler;

public static class TutarCozucu
{
    public static decimal? Coz(string? metin) =>
        decimal.TryParse((metin ?? "").Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                         CultureInfo.InvariantCulture, out var tutar)
            ? tutar
            : null;
}