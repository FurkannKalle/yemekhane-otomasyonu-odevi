using Yemekhane.Servisler;

namespace Yemekhane.Veri;

public class VeriKaynagi(OkumaDb okuma, YazmaDb yazma)
{
    public YemekhaneDb Okuma(OturumBilgisi oturum) =>
        oturum.SonYazma > DateTime.UtcNow.AddSeconds(-5) ? yazma : okuma;
}