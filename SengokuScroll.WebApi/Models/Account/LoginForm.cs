using System.ComponentModel.DataAnnotations;

namespace SengokuScroll.WebApi.Models.Account;

public class LoginForm
{
    [Required]
    public required string username { get; set; }

    [Required]
    public required string password { get; set; }
}
