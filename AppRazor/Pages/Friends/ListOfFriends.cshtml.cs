using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Models.Interfaces;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class ListOfFriendsModel : PageModel
    {
        readonly IAdminService _service = null;
        readonly IFriendsService _friendsService = null;
        readonly ILogger<ListOfFriendsModel> _logger = null;

        [BindProperty]
        public bool UseSeeds { get; set; } = true;
        
        public List<IFriend> Friends { get; set; }

        public int NrOfFriends { get; set; }

        public int NrOfPages { get; set; }
        public int PageSize { get; } = 10;

        public int ThisPageNr { get; set; } = 0;
        public int PrevPageNr { get; set; } = 0;
        public int NextPageNr { get; set; } = 0;
        public int NrVisiblePages { get; set; } = 0;

        [BindProperty]
        public string CountryFilter { get; set; } = null;

        public async Task<IActionResult> OnGet()
        {   
            if (int.TryParse(Request.Query["pagenr"], out int pagenr))
            {
                ThisPageNr = pagenr;
            }

            CountryFilter = Request.Query["country"];

            var resp = await _friendsService.ReadFriendsAsync(UseSeeds, false, CountryFilter, ThisPageNr, PageSize);
            Friends = resp.PageItems;
            NrOfFriends = resp.DbItemsCount;

            UpdatePagination(resp.DbItemsCount);

            return Page();
        }

        private void UpdatePagination(int nrOfItems)
        {
            //Pagination
            NrOfPages = (int)Math.Ceiling((double)nrOfItems / PageSize);
            PrevPageNr = Math.Max(0, ThisPageNr - 1);
            NextPageNr = Math.Min(NrOfPages - 1, ThisPageNr + 1);
            NrVisiblePages = Math.Min(10, NrOfPages);
        }

        public async Task<IActionResult> OnPostFilter()
        {
            var resp = await _friendsService.ReadFriendsAsync(UseSeeds, false, CountryFilter, ThisPageNr, PageSize);
            Friends = resp.PageItems;
            NrOfFriends = resp.DbItemsCount;

            UpdatePagination(resp.DbItemsCount);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteFriend(Guid friendId)
        {
            await _friendsService.DeleteFriendAsync(friendId);

            var resp = await _friendsService.ReadFriendsAsync(UseSeeds, false, CountryFilter, ThisPageNr, PageSize);
            Friends = resp.PageItems;
            NrOfFriends = resp.DbItemsCount;

            UpdatePagination(resp.DbItemsCount);

            return Page();
        }

        public ListOfFriendsModel(IFriendsService friendsService, ILogger<ListOfFriendsModel> logger)
        {
            _friendsService = friendsService;
            _logger = logger;
        }
    }
}
