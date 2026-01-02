using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppRazor.SeidoHelpers;
using Services.Interfaces;
using Models.Interfaces;
using Models.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Models;
using System.Net.WebSockets;
using System.Globalization;

namespace AppRazor.Pages.Friends;

public class EditFriendModel : PageModel
{
    private readonly IFriendsService _friendsService;
    private readonly IAddressesService _addressesService;
    private readonly IPetsService _petsService;
    private readonly IQuotesService _quotesService;

    public virtual IAddress Address { get; set; } = null;

    public virtual List<IPet> Pets { get; set; } = null;

    public virtual List<IQuote> Quotes { get; set; } = null;

    public IFriend Friend { get; set; }
    public SelectList CountrySelection { get; set; }

    [BindProperty]
    public FriendIM FriendInput { get; set; }

    [BindProperty]
    public PetIM NewPetIM { get; set; } = new PetIM();

    public EditFriendModel(IFriendsService friendsService, IAddressesService addressesService, IPetsService petsService, IQuotesService quotesService)
    {
        _friendsService = friendsService;
        _addressesService = addressesService;
        _petsService = petsService;
        _quotesService = quotesService;
    }

    public SeidoHelpers.ModelValidationResult ValidationResult { get; set; } = new SeidoHelpers.ModelValidationResult(false, null, null);
    public async Task<IActionResult> OnGet()
    {
        Guid _friendId = Guid.Parse(Request.Query["id"]);

        var response = await _friendsService.ReadFriendAsync(_friendId, false);
        FriendInput = new FriendIM(response.Item);

        RepopulateCountrySelection();

        return Page();
    }

    public async Task<IActionResult> OnPostSave()
    {
        var keys = new List<string>
        {
            "FriendInput.FirstName",
            "FriendInput.LastName",
            "FriendInput.Email"
        };

        var address = FriendInput.Address;
        bool addressChanged = address.StatusIM == StatusIM.Modified || address.StatusIM == StatusIM.Inserted;
        bool hasAddressData = !string.IsNullOrWhiteSpace(address.EditStreetAddress) ||
                              !string.IsNullOrWhiteSpace(address.EditCity) ||
                              !string.IsNullOrWhiteSpace(address.EditCountry) ||
                              (address.EditZipCode.HasValue && address.EditZipCode.Value > 0);
        bool shouldProcessAddress = addressChanged && (address.AddressId != Guid.Empty || hasAddressData);

        // Validera adress bara vid Save och adressen ska sparas
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
            ValidationResult = validationResult;
            RepopulateCountrySelection();
            return Page();
        }

        // Validera/spara adress bara om den har ändrats och antingen redan finns eller har fått nya värden
        if (shouldProcessAddress)
        {
            address.StreetAddress = address.EditStreetAddress;
            address.ZipCode = address.EditZipCode;
            address.City = address.EditCity;
            address.Country = address.EditCountry;

            if (address.AddressId != Guid.Empty)
            {
                await SaveAddress();
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
                    FriendsId = new List<Guid> { FriendInput.FriendId }
                };

                var createdAddressResp = await _addressesService.CreateAddressAsync(addressDto);
                var createdAddressId = createdAddressResp.Item.AddressId;

                var friendResp = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
                var friendToUpdateDto = new FriendCuDto(friendResp.Item)
                {
                    AddressId = createdAddressId
                };
                await _friendsService.UpdateFriendAsync(friendToUpdateDto);
            }
        }

        if (FriendInput.StatusIM == StatusIM.Modified)
        {
            var resp = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
            var friendToUpdate = resp.Item;

            friendToUpdate = FriendInput.UpdateModel(friendToUpdate);
            var friendToUpdateDto = new FriendCuDto(friendToUpdate);

            await _friendsService.UpdateFriendAsync(friendToUpdateDto);
        }


        foreach (var petIM in FriendInput.Pets)
        {
            if (petIM.StatusIM == StatusIM.Deleted)
            {
                await _petsService.DeletePetAsync(petIM.PetId);
            }
            else if (petIM.StatusIM == StatusIM.Modified)
            {
                var petResp = await _petsService.ReadPetAsync(petIM.PetId, false);
                var petToUpdate = petResp.Item;
                petToUpdate = petIM.UpdateModel(petToUpdate);
                var petToUpdateDto = new PetCuDto(petToUpdate);
                await _petsService.UpdatePetAsync(petToUpdateDto);
            }
        }

        foreach (var quoteIM in FriendInput.Quotes)
        {
            if (quoteIM.StatusIM == StatusIM.Deleted)
            {
                await _quotesService.DeleteQuoteAsync(quoteIM.QuoteId);
            }
            else if (quoteIM.StatusIM == StatusIM.Modified)
            {
                var quoteResp = await _quotesService.ReadQuoteAsync(quoteIM.QuoteId, false);
                var quoteToUpdate = quoteResp.Item;
                quoteToUpdate = quoteIM.UpdateModel(quoteToUpdate);
                var quoteToUpdateDto = new QuoteCuDto(quoteToUpdate);
                await _quotesService.UpdateQuoteAsync(quoteToUpdateDto);
            }
        }
        return Redirect($"~/Friends/ListOfFriends");
    }

    public async Task<IActionResult> OnPostUndo()
    {
        return RedirectToPage(new { id = FriendInput.FriendId });
    }

    public IActionResult OnPostDeletePet(Guid petId)
    {
        var pet = FriendInput.Pets.FirstOrDefault(a => a.PetId == petId);
        if (pet != null)
        {
            pet.StatusIM = StatusIM.Deleted;
        }

        RepopulateCountrySelection();

        return Page();
    }
    public IActionResult OnPostDeleteQuote(Guid quoteId)
    {
        FriendInput.Quotes.First(a => a.QuoteId == quoteId).StatusIM = StatusIM.Deleted;

        RepopulateCountrySelection();

        return Page();
    }

    public async Task<IActionResult> OnPostAddPet()
    {
        string[] keys = { "FriendInput.NewPet.Name" };

        if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
        {
            ValidationResult = validationResult;
            return Page();
        }

        var petDto = new PetCuDto()
        {
            PetId = null,
            Name = FriendInput.NewPet.Name,
            FriendId = FriendInput.FriendId
        };

        await _petsService.CreatePetAsync(petDto);

        // Reload friend to get updated Pets list
        var friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
        FriendInput = new FriendIM(friend.Item);

        RepopulateCountrySelection();

        return Page();

    }
    public async Task<IActionResult> OnPostAddQuote()
    {
        string[] keys = { "FriendInput.NewQuote.QuoteText", "FriendInput.NewQuote.Author" };

        if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
        {
            ValidationResult = validationResult;
            return Page();
        }

        var quoteDto = new QuoteCuDto()
        {
            QuoteId = null,
            Quote = FriendInput.NewQuote.QuoteText,
            Author = FriendInput.NewQuote.Author,
            FriendsId = new List<Guid> { FriendInput.FriendId }
        };

        await _quotesService.CreateQuoteAsync(quoteDto);

        var friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
        FriendInput = new FriendIM(friend.Item);

        RepopulateCountrySelection();

        return Page();
    }

    public IActionResult OnPostEditPet(Guid petId)
    {
        int index = FriendInput.Pets.FindIndex(p => p.PetId == petId);
        string[] keys = { $"FriendInput.Pets[{index}].editName" };

        if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
        {
            ValidationResult = validationResult;
            return Page();
        }

        var petIM = FriendInput.Pets.First(p => p.PetId == petId);
        if (petIM.StatusIM != StatusIM.Inserted)
            petIM.StatusIM = StatusIM.Modified;

        petIM.Name = petIM.EditName;

        RepopulateCountrySelection();

        return Page();
    }
    public IActionResult OnPostEditQuote(Guid quoteId)
    {
        int index = FriendInput.Quotes.FindIndex(q => q.QuoteId == quoteId);
        string[] keys = { $"FriendInput.Quotes[{index}].editQuoteText", $"FriendInput.Quotes[{index}].editAuthor" };

        if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
        {
            ValidationResult = validationResult;
            return Page();
        }
        var quoteIM = FriendInput.Quotes.First(q => q.QuoteId == quoteId);

        if (quoteIM.StatusIM != StatusIM.Inserted)
            quoteIM.StatusIM = StatusIM.Modified;

        quoteIM.QuoteText = quoteIM.EditQuoteText;
        quoteIM.Author = quoteIM.EditAuthor;

        RepopulateCountrySelection();

        return Page();
    }

    // Hjälpmetod för att spara adress
    private async Task<IAddress> SaveAddress()
    {
        var resp = await _addressesService.ReadAddressAsync(FriendInput.Address.AddressId, false);
        var addressToUpdate = resp.Item;

        addressToUpdate = FriendInput.Address.UpdateModel(addressToUpdate);
        var addressToUpdateDto = new AddressCuDto(addressToUpdate);

        await _addressesService.UpdateAddressAsync(addressToUpdateDto);

        return addressToUpdate;
    }

    #region Input Models
    public enum StatusIM { Unknown, Unchanged, Inserted, Modified, Deleted }
    
    public class FriendIM
    {
        public StatusIM StatusIM { get; set; }

        public Guid FriendId { get; set; }

        [Required(ErrorMessage = "Your friend must have a firstname")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Your friend must have a lastname")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Your friend must have an email")]
        public string Email { get; set; }

        //Väljer att inte göra Birthday required eftersom den kan vara null i modellen
        public DateTime? Birthday { get; set; }

        public AddressIM Address { get; set; } = new AddressIM();

        public List<PetIM> Pets { get; set; } = new List<PetIM>();
        public List<QuoteIM> Quotes { get; set; } = new List<QuoteIM>();

        public FriendIM() { }
        public FriendIM(IFriend model)
        {
            StatusIM = StatusIM.Unchanged;
            FriendId = model.FriendId;
            FirstName = model.FirstName;
            LastName = model.LastName;
            Email = model.Email;
            Birthday = model.Birthday;
            Address = model.Address != null ? new AddressIM(model.Address) : new AddressIM();
            Pets = model.Pets?.Select(m => new PetIM(m)).ToList() ?? new List<PetIM>();
            Quotes = model.Quotes?.Select(m => new QuoteIM(m)).ToList() ?? new List<QuoteIM>();
        }

        public IFriend UpdateModel(IFriend model)
        {
            model.FirstName = this.FirstName;
            model.LastName = this.LastName;
            model.Email = this.Email;
            model.Birthday = this.Birthday;

            return model;
        }

        public PetIM NewPet { get; set; } = new PetIM();
        public QuoteIM NewQuote { get; set; } = new QuoteIM();
        public AddressIM NewAddress { get; set; } = new AddressIM();

    }
    public class AddressIM
    {
        public StatusIM StatusIM { get; set; }

        public Guid AddressId { get; set; }

        public string StreetAddress { get; set; }

        public int? ZipCode { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        [Required(ErrorMessage = "Your friend must have a Street Address")]
        public string EditStreetAddress { get; set; }

        [Required(ErrorMessage = "Zip code is required")]
        [Range(1, 999999, ErrorMessage = "Zip code must be a positive number")]
        public int? EditZipCode { get; set; }

        [Required(ErrorMessage = "Your friend must have a City")]
        public string EditCity { get; set; }

        [Required(ErrorMessage = "Your friend must have a Country")]
        public string EditCountry { get; set; }

        public AddressIM() { }
        public AddressIM(AddressIM original)
        {
            StatusIM = StatusIM.Unchanged;
            AddressId = original.AddressId;
            StreetAddress = original.StreetAddress;
            ZipCode = original.ZipCode;
            City = original.City;
            Country = original.Country;

            EditStreetAddress = original.EditStreetAddress;
            EditZipCode = original.EditZipCode;
            EditCity = original.EditCity;
            EditCountry = original.EditCountry;
        }
        public AddressIM(IAddress model)
        {
            StatusIM = StatusIM.Unchanged;
            AddressId = model.AddressId;
            StreetAddress = model.StreetAddress;
            ZipCode = model.ZipCode;
            City = model.City;
            Country = model.Country;

            EditStreetAddress = model.StreetAddress;
            EditZipCode = model.ZipCode;
            EditCity = model.City;
            EditCountry = model.Country;
        }
        public IAddress UpdateModel(IAddress model)
        {
            model.AddressId = this.AddressId;
            model.StreetAddress = this.StreetAddress;
            model.ZipCode = this.ZipCode ?? 0;
            model.City = this.City;
            model.Country = this.Country;
            return model;
        }
        public AddressCuDto CreateCUdto() => new AddressCuDto()
        {
            AddressId = null,
            StreetAddress = this.StreetAddress,
        };
    }
    public class PetIM
    {
        public StatusIM StatusIM { get; set; }

        public Guid PetId { get; set; }

        [Required(ErrorMessage = "Pet must have a name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Pet must have a name")]
        public string EditName { get; set; }

        public PetIM() { }
        public PetIM(PetIM original)
        {
            StatusIM = StatusIM.Unchanged;
            PetId = original.PetId;
            Name = original.Name;

            EditName = original.EditName;
        }
        public PetIM(IPet model)
        {
            StatusIM = StatusIM.Unchanged;
            PetId = model.PetId;
            Name = EditName = model.Name;
        }
        public IPet UpdateModel(IPet model)
        {
            model.PetId = this.PetId;
            model.Name = this.Name;
            return model;
        }

        public PetCuDto CreateCUdto() => new PetCuDto()
        {
            PetId = null,
            Name = this.Name
        };
    }
    public class QuoteIM
    {
        public StatusIM StatusIM { get; set; }

        public Guid QuoteId { get; set; }

        [Required(ErrorMessage = "Quote must have text")]
        public string QuoteText { get; set; }

        [Required(ErrorMessage = "Quote must have an author")]
        public string Author { get; set; }

        [Required(ErrorMessage = "Quote must have text")]
        public string EditQuoteText { get; set; }

        [Required(ErrorMessage = "Quote must have an author")]
        public string EditAuthor { get; set; }

        public QuoteIM() { }
        public QuoteIM(QuoteIM original)
        {
            StatusIM = StatusIM.Unchanged;
            QuoteId = original.QuoteId;
            QuoteText = original.QuoteText;
            Author = original.Author;

            EditQuoteText = original.EditQuoteText;
            EditAuthor = original.EditAuthor;
        }
        public QuoteIM(IQuote model)
        {
            StatusIM = StatusIM.Unchanged;
            QuoteId = model.QuoteId;
            QuoteText = EditQuoteText = model.QuoteText;
            Author = EditAuthor = model.Author;
        }
        public IQuote UpdateModel(IQuote model)
        {
            model.QuoteId = this.QuoteId;
            model.QuoteText = this.QuoteText;
            model.Author = this.Author;
            return model;
        }
        public QuoteCuDto CreateCUdto() => new QuoteCuDto()
        {
            QuoteId = null,
            Quote = this.QuoteText,
            Author = this.Author
        };
    }
    #endregion

    private void RepopulateCountrySelection()
    {
        CountrySelection = new SelectList(new List<string>
        {
            "Denmark",
            "Finland",
            "Norway",
            "Sweden",
            "Other",
            "Unknown"
        }, FriendInput.Address.EditCountry);
    }

}
