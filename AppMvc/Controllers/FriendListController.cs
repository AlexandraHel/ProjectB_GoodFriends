using Microsoft.AspNetCore.Mvc;
using AppMvc.Models;
using Services.Interfaces;

namespace AppMvc.Controllers;
public class FriendListController : Controller
{
    private readonly IFriendsService _friendsService;

    public FriendListController(IFriendsService friendsService)
    {
        _friendsService = friendsService;
    }

    [HttpGet]
    public async Task<IActionResult> FriendList()
    {
        var vm = new FriendListViewModel();
              if (int.TryParse(Request.Query["pagenr"], out int _pagenr))
            {
                vm.ThisPageNr = _pagenr;
            }

           vm.CountryFilter = Request.Query["search"];

          //Delar upp länder för att markera checkboxar när sida laddas och filter finns
           if (!string.IsNullOrEmpty(vm.CountryFilter))
            {
                var countries = vm.CountryFilter.Split(',');
                vm.Denmark = countries.Contains("Denmark");
                vm.Finland = countries.Contains("Finland");
                vm.Norway = countries.Contains("Norway");
                vm.Sweden = countries.Contains("Sweden");
                vm.Unknown = countries.Contains("Unknown"); //string empty?
                vm.Other = countries.Contains("Other");
            }

            var resp = await _friendsService.ReadFriendsAsync(vm.UseSeeds, false, vm.CountryFilter, vm.ThisPageNr, vm.PageSize);
            vm.Friends = resp.PageItems;
            vm.NrOfFriends = resp.DbItemsCount;

            UpdatePagination(vm, resp.DbItemsCount);
       

        return View(vm);
        
    }
      [HttpPost]
      public async Task<IActionResult> Search(FriendListViewModel vm)
        {
            // Filtersträng av valda länder som skickas tillbaka som querystring
            var selectedCountries = new List<string>();
            
            if (vm.Denmark) selectedCountries.Add("Denmark");
            if (vm.Finland) selectedCountries.Add("Finland");
            if (vm.Norway) selectedCountries.Add("Norway");
            if (vm.Sweden) selectedCountries.Add("Sweden");
            if (vm.Unknown) selectedCountries.Add("Unknown");
            if (vm.Other) selectedCountries.Add("Other");
            
            vm.CountryFilter = selectedCountries.Count > 0 ? string.Join(",", selectedCountries) : null;
            
            return RedirectToAction("FriendList", new { pagenr = 0, search = vm.CountryFilter });
        }


        private void UpdatePagination(FriendListViewModel vm, int nrOfItems)
        {
            //Pagination
            vm.NrOfPages = (int)Math.Ceiling((double)nrOfItems / vm.PageSize);
            vm.PrevPageNr = Math.Max(0, vm.ThisPageNr - 1);
            vm.NextPageNr = Math.Min(vm.NrOfPages - 1, vm.ThisPageNr + 1);
            vm.NrVisiblePages = Math.Min(10, vm.NrOfPages);
        }
}