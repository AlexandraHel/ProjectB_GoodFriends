
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Models.Interfaces; //?
using global::Models.DTO;

namespace AppMvc.Models;

public class OverviewViewModel
{
        private readonly IAdminService _adminService;

        public string Country { get; set; }  //BindProperty?
        public IEnumerable<GstUsrInfoFriendsDto>? CountryInfo;
        public IEnumerable<GstUsrInfoFriendsDto>? FriendsInfo;
        public IEnumerable<GstUsrInfoPetsDto>? PetsInfo;
        public int FriendsWithoutCountry { get; set; }

        public OverviewViewModel(IAdminService adminService)
        {
            _adminService = adminService;
            
        }

     
}
