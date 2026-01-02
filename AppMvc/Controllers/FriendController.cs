using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AppMvc.Models;
using AppMvc.SeidoHelpers;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using Models.Interfaces;
using Models.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System.Net.WebSockets;
using System.Globalization;

namespace AppMvc.Controllers
{
    public class FriendController : Controller
    {
        private readonly IFriendsService _friendsService;
        private readonly IAddressesService _addressesService;
        private readonly IPetsService _petsService;
        private readonly IQuotesService _quotesService;

        public FriendController(IFriendsService friendsService, IAddressesService addressesService,
            IPetsService petsService,
            IQuotesService quotesService)
        {
            _friendsService = friendsService;
            _addressesService = addressesService;
            _petsService = petsService;
            _quotesService = quotesService;
        }

       // public SeidoHelpers.ModelValidationResult ValidationResult { get; set; } = new SeidoHelpers.ModelValidationResult(false, null, null);

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

        [HttpGet]
        public async Task<IActionResult> EditFriend(string id)
        {
            Guid friendId = Guid.Parse(id);
            var response = await _friendsService.ReadFriendAsync(friendId, false);
            var vm = new FriendViewModel()
            {
                FriendInput = new FriendViewModel.FriendIM(response.Item)
            };
            //vm.Friend = response.Item;

            //vm.Pets = vm.Friend.Pets?.ToList();
            //vm.Quotes = vm.Friend.Quotes?.ToList();
            //vm.Address = vm.Friend.Address;
            
            RepopulateCountrySelection(vm);
            return View(vm);
        }

        [HttpPost]
        public IActionResult DeletePet(Guid petId, FriendViewModel vm)
        {
            vm.FriendInput.Pets.First(p => p.PetId == petId).StatusIM = FriendViewModel.StatusIM.Deleted;

            return View("EditFriend",vm);
        }

        [HttpPost]
        public IActionResult DeleteQuote(Guid quoteId, FriendViewModel vm)
        {
            vm.FriendInput.Quotes.First(q => q.QuoteId == quoteId).StatusIM = FriendViewModel.StatusIM.Deleted;

            return View("EditFriend", vm);
        }

        
        [HttpPost]
        public async Task<IActionResult> AddPet(FriendViewModel vm)
        {
            string[] keys = { "FriendInput.NewPet.Name"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                vm.ValidationResult = validationResult;
                return View("EditFriend", vm);
            }
            vm.FriendInput.NewPet.StatusIM = FriendViewModel.StatusIM.Inserted;
            vm.FriendInput.NewPet.PetId = Guid.NewGuid();
            vm.FriendInput.Pets.Add(new FriendViewModel.PetIM(vm.FriendInput.NewPet));
            /*
            var petDto = new PetCuDto()
            {
                PetId = null,
                Name = vm.Friend.NewPet.Name,
                FriendId = vm.Friend.FriendId
            };

            await _petsService.CreatePetAsync(petDto);
            var friend = await _friendsService.ReadFriendAsync(vm.Friend.FriendId, false);
            vm.Friend = new FriendIM(friend.Item);*/

            RepopulateCountrySelection(vm);

            return View("EditFriend", vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddQuote(FriendViewModel vm)
        {
            string[] keys = { "FriendInput.NewQuote.QuoteText", "FriendInput.NewQuote.Author" };

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                vm.ValidationResult = validationResult;
                return View("EditFriend", vm);
            }
                 /*
            var quoteDto = new QuoteCuDto()
            {
                QuoteId = null,
                Author = vm.Friend.NewQuote.Author,
                Text = vm.Friend.NewQuote.QuoteText,
                FriendId = vm.Friend.FriendId
            };
       
            await _quotesService.CreateQuoteAsync(quoteDto);

            var friend = await _friendsService.ReadFriendAsync(vm.Friend.FriendId, false);
            vm.Friend = new FriendIM(friend.Item);*/

            vm.FriendInput.NewQuote.StatusIM = FriendViewModel.StatusIM.Inserted;
            vm.FriendInput.NewQuote.QuoteId = Guid.NewGuid();
            vm.FriendInput.Quotes.Add(new FriendViewModel.QuoteIM(vm.FriendInput.NewQuote));

            RepopulateCountrySelection(vm);
            return View("EditFriend", vm);
        }

        public async Task<IActionResult> EditPet(Guid petId, FriendViewModel vm)
        {
            int index = vm.FriendInput.Pets.FindIndex(p => p.PetId == petId);
            string[] keys = { $"FriendInput.Pets[{index}].EditName" };

            if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
            {
                vm.ValidationResult = validationResult;
                return View("EditFriend", vm);
            }

            var petIM = vm.FriendInput.Pets.First(p => p.PetId == petId);

            if (petIM.StatusIM != FriendViewModel.StatusIM.Inserted)
            {
                petIM.StatusIM = FriendViewModel.StatusIM.Modified;
            }

            petIM.Name = petIM.EditName;

            RepopulateCountrySelection(vm);

            return View("EditFriend", vm);
        }
        public IActionResult EditQuote(Guid quoteId, FriendViewModel vm)
        {
            int index = vm.FriendInput.Quotes.FindIndex(q => q.QuoteId == quoteId);
            string[] keys = { $"FriendInput.Quotes[{index}].EditQuoteText", $"FriendInput.Quotes[{index}].EditAuthor" };

            if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
            {
                vm.ValidationResult = validationResult;
                return View("EditFriend", vm);
            }
            var quoteIM = vm.FriendInput.Quotes.First(q => q.QuoteId == quoteId);

            if (quoteIM.StatusIM != FriendViewModel.StatusIM.Inserted)
                quoteIM.StatusIM = FriendViewModel.StatusIM.Modified;

            quoteIM.QuoteText = quoteIM.EditQuoteText;
            quoteIM.Author = quoteIM.EditAuthor;

            RepopulateCountrySelection(vm);

            return View("EditFriend", vm);
        }

       /* private async Task<IAddress> SaveAddress()
        {
            var resp = await _addressesService.ReadAddressAsync(FriendInput.Address.AddressId, false);
            var addressToUpdate = resp.Item;

            addressToUpdate = FriendInput.Address.UpdateModel(addressToUpdate);
            var addressToUpdateDto = new AddressCuDto(addressToUpdate);

            await _addressesService.UpdateAddressAsync(addressToUpdateDto);

            return addressToUpdate;
        }*/

        private void RepopulateCountrySelection(FriendViewModel vm)
        {
            vm.CountrySelection = new SelectList(new List<string>
            {
                "Denmark",
                "Finland",
                "Norway",
                "Sweden",
                "Other",
                "Unknown"
            }, vm.FriendInput.Address.EditCountry);
        }
        
    }
    
}