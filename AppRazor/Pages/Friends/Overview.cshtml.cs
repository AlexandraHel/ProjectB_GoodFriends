using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Models.Interfaces; //?
using global::Models.DTO;
using System.Collections.Generic;

namespace AppRazor.Pages.Friends
{
    public class OverviewModel : PageModel
    {
        private readonly IAddressesService _addressesService;
        private readonly IAdminService _adminService;
      
        public IEnumerable<GstUsrInfoFriendsDto>? CountryInfo;
        public int FriendsWithoutCountry { get; set; }
        public List<IAddress> Addresses { get; set; } = new List<IAddress>();
     
        public async Task<IActionResult> OnGet()
        {
            var info = await _adminService.GuestInfoAsync();
         
            // Vänner med Country - gruppera och summera
            CountryInfo = info.Item.Friends
                .Where(f => !string.IsNullOrEmpty(f.Country))
                .GroupBy(f => f.Country)
                .Select(g => new GstUsrInfoFriendsDto
                {
                    Country = g.Key,
                    NrFriends = g.Sum(f => f.NrFriends)
                });
            
            // Vänner utan Country
            FriendsWithoutCountry = info.Item.Friends
                .Where(f => string.IsNullOrEmpty(f.Country))
                .Sum(f => f.NrFriends);
         
            return Page();
        }

        public OverviewModel(IAddressesService addressesService, IAdminService adminService)
        {
            _addressesService = addressesService;
            _adminService = adminService;
        }
    }
}
