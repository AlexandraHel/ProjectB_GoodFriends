using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AppMvc.Models;
using Services.Interfaces;

namespace AppMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    //private readonly IAddressesService _addressesService;
    private readonly IAdminService _adminService;

    public HomeController(ILogger<HomeController> logger, IAddressesService addressesService, IAdminService adminService)
    {
        _logger = logger;
        //_addressesService = addressesService;
        _adminService = adminService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Seed()
    {
        return View();
    }

    public async Task<IActionResult> Overview()
        {
            var viewModel = new OverviewViewModel();
            //var addresses = await _addressesService.ReadAddressesAsync(true, false, "Denmark", 0 ,50);
            var info = await _adminService.GuestInfoAsync();
            viewModel.CountryInfo = info.Item.Friends.Where(f=> f.Country == "Denmark");
            return View(viewModel);
        }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
