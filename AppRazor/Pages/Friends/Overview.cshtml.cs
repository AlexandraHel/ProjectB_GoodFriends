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
        //private readonly IFriendsService _friendsService;
        //public List<IFriend> Friends { get; set; } = new List<IFriend>();
        public IEnumerable<GstUsrInfoFriendsDto>? CountryInfo;
     
        public async Task<IActionResult> OnGet()
        {
            var addresses = await _addressesService.ReadAddressesAsync(true, false, "Denmark", 0 ,50);
            var info = await _adminService.GuestInfoAsync();
            CountryInfo = info.Item.Friends.Where(f=> f.Country == "Denmark");
            return Page();
        }

        public OverviewModel(IAddressesService addressesService, IAdminService adminService
                             /*IFriendsService friendsService,
                             IPetsService petsService*/)
        {
            _addressesService = addressesService;
            _adminService = adminService;
            //_friendsService = friendsService;
        }
    }
}
