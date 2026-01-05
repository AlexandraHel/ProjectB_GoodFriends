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
            
            RepopulateCountrySelection(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Undo(FriendViewModel vm)
        {
            var response = await _friendsService.ReadFriendAsync(vm.FriendInput.FriendId, false);
        
            vm.FriendInput = new FriendViewModel.FriendIM(response.Item);
            ModelState.Clear();

            return View("EditFriend", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Save(FriendViewModel vm)
        {
            var keys = new List<string> {
                "FriendInput.FirstName",
                "FriendInput.LastName",
                "FriendInput.Email"
            };
            
            //kollar på om adressen har ändrats och om den ska sparas(om guid var tom från början eller om några fält har fått värden)
            var address = vm.FriendInput.Address;
            bool addressChanged = address.StatusIM == FriendViewModel.StatusIM.Modified || address.StatusIM == FriendViewModel.StatusIM.Inserted;
            bool hasAddressData = !string.IsNullOrWhiteSpace(address.EditStreetAddress) ||
                                !string.IsNullOrWhiteSpace(address.EditCity) ||
                                !string.IsNullOrWhiteSpace(address.EditCountry) ||
                                (address.EditZipCode.HasValue && address.EditZipCode.Value > 0);
            bool shouldProcessAddress = addressChanged && (address.AddressId != Guid.Empty || hasAddressData);

            // Validerar adress bara vid Save och adressen ska sparas (Om tomt Guid och inga ändringar skett ska den inte valideras)
            if (shouldProcessAddress)
            {
                keys.AddRange(new[]
                {
                    "FriendInput.Address.EditStreetAddress",
                    "FriendInput.Address.EditZipCode",
                    "FriendInput.Address.EditCity",
                    "FriendInput.Address.EditCountry"
                });
            }
           
            if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys.ToArray()))
            {
                vm.ValidationResult = validationResult;
                RepopulateCountrySelection(vm);
                return View("EditFriend", vm);
            }

            if (shouldProcessAddress)
            {
                address.StreetAddress = address.EditStreetAddress;
                address.ZipCode = address.EditZipCode;
                address.City = address.EditCity;
                address.Country = address.EditCountry;

                if (address.AddressId != Guid.Empty)
                {
                    await SaveAddress(vm);
                }
                else
                {
                    var addressDto = new AddressCuDto
                    {
                        AddressId = null,
                        StreetAddress = address.StreetAddress,
                        ZipCode = address.ZipCode ?? 0,
                        City = address.City,
                        Country = address.Country,
                        FriendsId = new List<Guid> { vm.FriendInput.FriendId }
                    };

                    var createdAddressResp = await _addressesService.CreateAddressAsync(addressDto);
                    var createdAddressId = createdAddressResp.Item.AddressId;

                    var friendResp = await _friendsService.ReadFriendAsync(vm.FriendInput.FriendId, false);
                    var friendToUpdateDto = new FriendCuDto(friendResp.Item)
                    {
                        AddressId = createdAddressId
                    };
                    await _friendsService.UpdateFriendAsync(friendToUpdateDto);
                }
            }
            
            if (vm.FriendInput.StatusIM == FriendViewModel.StatusIM.Modified)
            {
                var resp = await _friendsService.ReadFriendAsync(vm.FriendInput.FriendId, false);
                var friendToUpdate = resp.Item;

                friendToUpdate = vm.FriendInput.UpdateModel(friendToUpdate);
                var friendToUpdateDto = new FriendCuDto(friendToUpdate);

                await _friendsService.UpdateFriendAsync(friendToUpdateDto);
            }

            foreach (var petIM in vm.FriendInput.Pets)
            {
                if (petIM.StatusIM == FriendViewModel.StatusIM.Deleted)
                {
                    await _petsService.DeletePetAsync(petIM.PetId);
                }
       
                else if (petIM.StatusIM == FriendViewModel.StatusIM.Modified)
                {
                    var petResp = await _petsService.ReadPetAsync(petIM.PetId, false);
                    var petToUpdate = petResp.Item;
                    petToUpdate = petIM.UpdateModel(petToUpdate);
                    var petToUpdateDto = new PetCuDto(petToUpdate);
                    await _petsService.UpdatePetAsync(petToUpdateDto);
                }
            }

            foreach (var quoteIM in vm.FriendInput.Quotes)
            {
                if (quoteIM.StatusIM == FriendViewModel.StatusIM.Deleted)
                {
                    await _quotesService.DeleteQuoteAsync(quoteIM.QuoteId);
                }
                else if (quoteIM.StatusIM == FriendViewModel.StatusIM.Modified)
                {
                    var quoteResp = await _quotesService.ReadQuoteAsync(quoteIM.QuoteId, false);
                    var quoteToUpdate = quoteResp.Item;
                    quoteToUpdate = quoteIM.UpdateModel(quoteToUpdate);
                    var quoteToUpdateDto = new QuoteCuDto(quoteToUpdate);
                    await _quotesService.UpdateQuoteAsync(quoteToUpdateDto);
                }
            }
            return Redirect($"~/Friend/ViewFriend?id={vm.FriendInput.FriendId}");
        }

        [HttpPost]
        public IActionResult DeletePet(Guid petId, FriendViewModel vm)
        {
            vm.FriendInput.Pets.First(p => p.PetId == petId).StatusIM = FriendViewModel.StatusIM.Deleted;
            RepopulateCountrySelection(vm);
            return View("EditFriend",vm);
        }

        [HttpPost]
        public IActionResult DeleteQuote(Guid quoteId, FriendViewModel vm)
        {
            vm.FriendInput.Quotes.First(q => q.QuoteId == quoteId).StatusIM = FriendViewModel.StatusIM.Deleted;
            RepopulateCountrySelection(vm);
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
        
            var petDto = new PetCuDto()
            {
                PetId = null,
                Name = vm.FriendInput.NewPet.Name,
                FriendId = vm.FriendInput.FriendId
            };

            await _petsService.CreatePetAsync(petDto);
            var friend = await _friendsService.ReadFriendAsync(vm.FriendInput.FriendId, false);
            vm.FriendInput = new FriendViewModel.FriendIM(friend.Item);

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
            var quoteDto = new QuoteCuDto()
            {
                QuoteId = null,
                Author = vm.FriendInput.NewQuote.Author,
                Quote = vm.FriendInput.NewQuote.QuoteText,
                FriendsId = new List<Guid> { vm.FriendInput.FriendId }
            };
       
            await _quotesService.CreateQuoteAsync(quoteDto);

            var friend = await _friendsService.ReadFriendAsync(vm.FriendInput.FriendId, false);
            vm.FriendInput = new FriendViewModel.FriendIM(friend.Item);

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

        private async Task<IAddress> SaveAddress(FriendViewModel vm)
        {
            var resp = await _addressesService.ReadAddressAsync(vm.FriendInput.Address.AddressId, false);
            var addressToUpdate = resp.Item;

            addressToUpdate = vm.FriendInput.Address.UpdateModel(addressToUpdate);
            var addressToUpdateDto = new AddressCuDto(addressToUpdate);

            await _addressesService.UpdateAddressAsync(addressToUpdateDto);

            return addressToUpdate;
        }

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