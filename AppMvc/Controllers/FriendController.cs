using Microsoft.AspNetCore.Mvc;
using AppMvc.Models;
using Services.Interfaces;

namespace AppMvc.Controllers
{
    public class FriendController : Controller
    {
        private readonly IFriendsService _friendsService;

        public FriendController(IFriendsService friendsService)
        {
            _friendsService = friendsService;
        }

        [HttpGet]
        public async Task<IActionResult> ViewFriend(string id)
        {
            Guid friendId = Guid.Parse(id);
            var response = await _friendsService.ReadFriendAsync(friendId, false);
            var vm = new FriendViewModel();
            vm.Friend = response.Item;

            vm.Pets = vm.Friend.Pets?.ToList();
            vm.Quotes = vm.Friend.Quotes?.ToList();
            vm.Address = vm.Friend.Address;

            return View(vm);
        }
    }
}