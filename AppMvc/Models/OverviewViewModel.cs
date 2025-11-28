
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Models.Interfaces; //?
using global::Models.DTO;

namespace AppMvc.Models;

public class OverviewViewModel
{
     public IEnumerable<GstUsrInfoFriendsDto>? CountryInfo;
}
