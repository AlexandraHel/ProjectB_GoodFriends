 using Services.Interfaces;
 using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;
using AppMvc.Controllers;

namespace AppMvc.Models;
 public class SeedViewModel
 {
       public IAdminService _admin_service = null;
        //readonly ILogger<SeedViewModel> _logger = null;

        public int NrOfFriends => nrOfFriends().Result;
        public async Task<int> nrOfFriends()
        {
            var info = await _admin_service.GuestInfoAsync();
            return info.Item.Db.NrSeededFriends + info.Item.Db.NrUnseededFriends;
        }
        
        [BindProperty]
        [Required (ErrorMessage = "You must enter nr of items to seed")]
        public int NrOfItemsToSeed { get; set; } = 100;

        [BindProperty]
        public bool RemoveSeeds { get; set; } = true;
      
   public async Task RemoveSeedsAsync()
    {
        await _admin_service.RemoveSeedAsync(true);
        await _admin_service.RemoveSeedAsync(false);
    }
    
    public async Task SeedDataAsync()
    {
        await _admin_service.SeedAsync(NrOfItemsToSeed);
    }
    public SeedViewModel(IAdminService admin_service/*ILogger<SeedViewModel> logger*/)
        {
            _admin_service = admin_service;
            //_logger = logger;
        }

}
