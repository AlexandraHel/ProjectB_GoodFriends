
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Models.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppRazor.Pages.Friends;

public class ViewFriendModel: PageModel
{
    private readonly IFriendsService _friendsService;
  
    public virtual IAddress Address { get; set; } = null;

    public virtual List<IPet> Pets { get; set; } = null;

    public virtual List<IQuote> Quotes { get; set; } = null;

    public IFriend Friend { get; set; }

        public async Task<IActionResult> OnGet(string id)
    {
            Guid _friendId = Guid.Parse(id);
            var response = await _friendsService.ReadFriendAsync(_friendId, false);
            Friend = response.Item;
        
                Pets = Friend.Pets?.ToList();
                Quotes = Friend.Quotes?.ToList();
                Address = Friend.Address;

            return Page();
    }

    public ViewFriendModel(IFriendsService friendsService)
    {
        _friendsService = friendsService;
    }
}