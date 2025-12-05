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
        private readonly IAdminService _adminService;
      
        public IEnumerable<GstUsrInfoFriendsDto>? CountryInfo;
        public int FriendsWithoutCountry { get; set; }
     
        public async Task<IActionResult> OnGet()
        {
            var info = await _adminService.GuestInfoAsync();
         
            CountryInfo = info.Item.Friends
                .Where(f => f.City == null && f.Country != null)
                .GroupBy(f => f.Country)
                .Select(g => new GstUsrInfoFriendsDto
                {
                    Country = g.Key,
                    NrFriends = g.Sum(f => f.NrFriends)
                });
            
            FriendsWithoutCountry = info.Item.Friends
                .Where(f => f.Country == null)
                .Sum(f => f.NrFriends);
         
            return Page();
        }

        public OverviewModel(IAdminService adminService)
        {
            _adminService = adminService;
            
        }
    }
}
