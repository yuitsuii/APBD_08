using System.ComponentModel.DataAnnotations;

namespace Tutorial8.Models.DTOs;


//client dto, seemingly only DTO which requires these annotations since it is the only one on which we use POST requests
public class ClientDTO
{
    public int IdClient { get; set; }

    [Required, MaxLength(120)]
    public string FirstName { get; set; }

    [Required, MaxLength(120)]
    public string LastName { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [Phone]
    public string Telephone { get; set; }

    [Required, RegularExpression(@"^\d{11}$")] 
    public string Pesel { get; set; }
}