using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Models.Interfaces;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class ListOfFriendsModel : PageModel
    {
        readonly IFriendsService _friendsService = null;
        //readonly ILogger<ListOfFriendsModel> _logger = null;
        
        public List<IFriend> Friends { get; set; }

        public int NrOfFriends { get; set; }

        public int NrOfPages { get; set; }
        public int PageSize { get; } = 10;

        public int ThisPageNr { get; set; } = 0;
        public int PrevPageNr { get; set; } = 0;
        public int NextPageNr { get; set; } = 0;
        public int NrVisiblePages { get; set; } = 0;

        
        [BindProperty] //behöver inte vara bindproperty då valet inte finns längre?
        public bool UseSeeds { get; set; } = true; 

        [BindProperty]
        public string CountryFilter { get; set; } = null;

        [BindProperty]
        public bool Denmark { get; set; }
        
        [BindProperty]
        public bool Finland { get; set; }
        
        [BindProperty]
        public bool Norway { get; set; }
        
        [BindProperty]
        public bool Sweden { get; set; }
        
        [BindProperty]
        public bool Unknown { get; set; }  //ska den användas på detta sätt eller bara tom?
         
        [BindProperty]        
        public bool Other { get; set; }

        public async Task<IActionResult> OnGet()
        {   
            if (int.TryParse(Request.Query["pagenr"], out int _pagenr))
            {
                ThisPageNr = _pagenr;
            }

           CountryFilter = Request.Query["search"];

          //Delar upp länder för att markera checkboxar när sida laddas och filter finns
           if (!string.IsNullOrEmpty(CountryFilter))
            {
                var countries = CountryFilter.Split(',');
                Denmark = countries.Contains("Denmark");
                Finland = countries.Contains("Finland");
                Norway = countries.Contains("Norway");
                Sweden = countries.Contains("Sweden");
                Unknown = countries.Contains("Unknown"); //string empty?

                //Other = countries.Contains(string.Empty);
            }

            var resp = await _friendsService.ReadFriendsAsync(UseSeeds, false, CountryFilter, ThisPageNr, PageSize);
            Friends = resp.PageItems;
            NrOfFriends = resp.DbItemsCount;

            UpdatePagination(resp.DbItemsCount);

            return Page();
        }

        public async Task<IActionResult> OnPostSearch()
        {
            // Filtersträng av valda länder som skickas tillbaka som querystring
            var selectedCountries = new List<string>();
            
            if (Denmark) selectedCountries.Add("Denmark");
            if (Finland) selectedCountries.Add("Finland");
            if (Norway) selectedCountries.Add("Norway");
            if (Sweden) selectedCountries.Add("Sweden");
            if (Unknown) selectedCountries.Add("Unknown");
            //if (Other) selectedCountries.Add("Other");
            
            CountryFilter = selectedCountries.Count > 0 ? string.Join(",", selectedCountries) : null;
            
            return RedirectToPage(new { pagenr = 0, search = CountryFilter });
        }

        
        private void UpdatePagination(int nrOfItems)
        {
            //Pagination
            NrOfPages = (int)Math.Ceiling((double)nrOfItems / PageSize);
            PrevPageNr = Math.Max(0, ThisPageNr - 1);
            NextPageNr = Math.Min(NrOfPages - 1, ThisPageNr + 1);
            NrVisiblePages = Math.Min(10, NrOfPages);
        }


        public ListOfFriendsModel(IFriendsService friendsService)
        {
            _friendsService = friendsService;
        }
    }
}
