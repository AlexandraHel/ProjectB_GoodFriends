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

    public IActionResult Seed()
    {
        var vm = new SeedViewModel(_adminService);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> OnPost(SeedViewModel vm)
        {
            if (ModelState.IsValid)
            {
                if (vm.RemoveSeeds)
                {
                    await vm.RemoveSeedsAsync();
                    await vm.RemoveSeedsAsync();
                }
                await vm.SeedDataAsync();

               return Redirect($"~/Overview/Overview");
            }
            return View(vm);
        }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
