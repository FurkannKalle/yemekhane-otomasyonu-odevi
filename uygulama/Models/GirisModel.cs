using System.ComponentModel.DataAnnotations;

namespace Yemekhane.Models;

public class GirisModel
{
    [Required(ErrorMessage = "E-posta girin.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Eposta { get; set; } = "";

    [Required(ErrorMessage = "Şifre girin.")]
    public string Sifre { get; set; } = "";
}