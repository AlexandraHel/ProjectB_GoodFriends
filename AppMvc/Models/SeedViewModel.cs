 using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AppMvc.Models;

public class SeedViewModel
{
    public int NrOfFriends { get; set; }

    
    [BindProperty]
    [Required(ErrorMessage = "You must enter nr of items to seed")]
    public int NrOfItemsToSeed { get; set; } = 100;

    [BindProperty]
    public bool RemoveSeeds { get; set; } = true;
}
