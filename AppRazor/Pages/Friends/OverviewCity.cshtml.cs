using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Models.DTO;

namespace AppRazor.Pages.Friends
{
    public class OverviewCityModel : PageModel
    {
        private readonly IAdminService _adminService;
      
        public IEnumerable<GstUsrInfoFriendsDto>? FriendsInfo;
        public IEnumerable<GstUsrInfoPetsDto>? PetsInfo;
     
        public async Task<IActionResult> OnGet(string country)
        {
            var info = await _adminService.GuestInfoAsync();
         
            FriendsInfo = info.Item.Friends
                .Where(f => f.Country == country && f.City != null)
                .GroupBy(f => f.City)
                .Select(g => new GstUsrInfoFriendsDto
                {
                    City = g.Key,
                    Country = country,
                    NrFriends = g.Sum(f => f.NrFriends)
                });
            
            PetsInfo = info.Item.Pets
                .Where(p => p.Country == country && p.City != null)
                .GroupBy(p => p.City)
                .Select(g => new GstUsrInfoPetsDto
                {
                    City = g.Key,
                    Country = country,
                    NrPets = g.Sum(p => p.NrPets)
                });
         
            return Page();
        }

        public OverviewCityModel(IAdminService adminService)
        {
            _adminService = adminService;
        }
    }
}
