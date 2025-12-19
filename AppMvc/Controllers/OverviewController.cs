
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
namespace AppMvc.Controllers;

using Models;
using global::Models.DTO;

public class OverviewController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IFriendsService _friendsService;
    public OverviewController(IAdminService adminService, IFriendsService friendsService)
    {
        _adminService = adminService;
        _friendsService = friendsService;
    }

    [HttpGet]
    public async Task<IActionResult> Overview()
    {
        var vm = new OverviewViewModel(_adminService);
        var info = await _adminService.GuestInfoAsync();

        vm.CountryInfo = info.Item.Friends
            .Where(f => f.City == null && f.Country != null)
            .GroupBy(f => f.Country)
            .Select(g => new GstUsrInfoFriendsDto
            {
                Country = g.Key,
                NrFriends = g.Sum(f => f.NrFriends)
            });

        vm.FriendsWithoutCountry = _friendsService.ReadFriendsAsync(true, true, "Unknown", 0, 10).Result.DbItemsCount;

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> OverviewCity(string country)
    {
        var info = await _adminService.GuestInfoAsync();
        var vm = new OverviewViewModel(_adminService);
        vm.Country = country;

        vm.FriendsInfo = info.Item.Friends
            .Where(f => f.Country == country && f.City != null)
            .GroupBy(f => f.City)
            .Select(g => new GstUsrInfoFriendsDto
            {
                City = g.Key,
                Country = country,
                NrFriends = g.Sum(f => f.NrFriends)
            });

        vm.PetsInfo = info.Item.Pets
            .Where(p => p.Country == country && p.City != null)
            .GroupBy(p => p.City)
            .Select(g => new GstUsrInfoPetsDto
            {
                City = g.Key,
                Country = country,
                NrPets = g.Sum(p => p.NrPets)
            });

        return View(vm);
    }
}