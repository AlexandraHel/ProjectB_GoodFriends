using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AppMvc.Models;
using Services.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AppMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAdminService _adminService;

    public HomeController(ILogger<HomeController> logger, IAdminService adminService)
    {
        _logger = logger;
        _adminService = adminService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Seed()
    {
        int nrOfFriends = await NrOfFriends();
        var vm = new SeedViewModel
        {
            NrOfFriends = nrOfFriends
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> SeedData(SeedViewModel vm)
    {
        if (ModelState.IsValid)
        {
            if (vm.RemoveSeeds)
            {
                await _adminService.RemoveSeedAsync(true);
                await _adminService.RemoveSeedAsync(false);
            }
            await _adminService.SeedAsync(vm.NrOfItemsToSeed);

            return Redirect($"~/Overview/Overview");
        }
        
        // Repopulate NrOfFriends on validation error
       // var info = await _adminService.GuestInfoAsync();
        //vm.NrOfFriends = info.Item.Db.NrSeededFriends + info.Item.Db.NrUnseededFriends;
        return View("Seed", vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

        private async Task<int> NrOfFriends()
        {
            var info = await _adminService.GuestInfoAsync();
            //return    
            return info.Item.Db.NrSeededFriends + info.Item.Db.NrUnseededFriends;
        }
}
