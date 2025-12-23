using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppRazor.SeidoHelpers;
using Services.Interfaces;
using Models.Interfaces;
using Models.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Models;
using System.Net.WebSockets;

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

    //InputModel (IM) is locally declared classes that contains ONLY the properties of the Model
    //that are bound to the <form> tag
    //EVERY property must be bound to an <input> tag in the <form>
    public IFriend Friend { get; set; }
    public SelectList CountrySelection { get; set; }

    [BindProperty]
    public FriendIM FriendInput { get; set; }

    [BindProperty]
    public PetIM NewPetIM { get; set; } = new PetIM();

    //ny input model för ny vän? pet? quote?
    //[BindProperty]
    //public PetIM PetIM { get; set; }

    //I also use BindProperty to keep between several posts, bound to hidden <input> field
    //[BindProperty]
    //public string PageHeader { get; set; } //Behövs denna?

    public EditFriendModel(IFriendsService friendsService, IAddressesService addressesService, IPetsService petsService, IQuotesService quotesService)
    {
        _friendsService = friendsService;
        _addressesService = addressesService;
        _petsService = petsService;
        _quotesService = quotesService;
    }

    public SeidoHelpers.ModelValidationResult ValidationResult { get; set; } = new SeidoHelpers.ModelValidationResult(false, null, null);
    //  public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, null, null);
    public async Task<IActionResult> OnGet()
    {
        StatusIM statusIM; //behövs inte?
        Guid _friendId = Guid.Parse(Request.Query["id"]);

        var response = await _friendsService.ReadFriendAsync(_friendId, false);
        FriendInput = new FriendIM(response.Item);

        RepopulateCountryselection();
        /*Friend = response.Item;

            Pets = Friend.Pets?.ToList();
            Quotes = Friend.Quotes?.ToList();
            Address = Friend.Address;*/

        return Page();
    }

    public async Task<IActionResult> OnPostSave()
    {
        string[] keys = {"FriendInput.FirstName",
                              "FriendInput.LastName",
                              "FriendInput.Email"
                              };

        if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
        {
            ValidationResult = validationResult;
            RepopulateCountryselection();
            return Page();
        }

        if (FriendInput.Address.StatusIM == StatusIM.Modified && FriendInput.Address.AddressId != Guid.Empty)
        {
            string[] keysAddress = {"FriendInput.Address.StreetAddress",
                              "FriendInput.Address.City",
                              "FriendInput.Address.Country"
                              };
            if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult addressValidationResult, keysAddress))
            {
                ValidationResult = addressValidationResult;
                RepopulateCountryselection();
                return Page();
            }
            await SaveAddress();
        }

        if (FriendInput.StatusIM == StatusIM.Modified)
        {
            var resp = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
            var friendToUpdate = resp.Item;

            //Update the friend with the values from the InputModel
            friendToUpdate = FriendInput.UpdateModel(friendToUpdate);
            var friendToUpdateDto = new FriendCuDto(friendToUpdate);
            //Save the updated friend to database
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
         /*Martins lösning:
            var mg = await _mg_service.ReadMusicGroupAsync(MusicGroupInput.MusicGroupId, false);

            //Repopulate the InputModel
            MusicGroupInput = new MusicGroupIM(mg.Item);
            
            //Clear ModelState to ensure the page displays the updated values
            ModelState.Clear();
            
            return Page();*/
    }

    public IActionResult OnPostDeletePet(Guid petId)
    {
        var pet = FriendInput.Pets.FirstOrDefault(a => a.PetId == petId);
        if (pet != null)
        {
            pet.StatusIM = StatusIM.Deleted;
        }

        RepopulateCountryselection();

        return Page();
    }
    public IActionResult OnPostDeleteQuote(Guid quoteId)
    {
        //Set the Artist as deleted, it will not be rendered
        FriendInput.Quotes.First(a => a.QuoteId == quoteId).StatusIM = StatusIM.Deleted;

        RepopulateCountryselection();

        return Page();
    }

    public async Task<IActionResult> OnPostAddPet()
    {
        string[] keys = { "FriendInput.NewPet.Name" }; //Behövs inte om det bara är Name som ska valideras?

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

        RepopulateCountryselection();

        return Page();

    }
    public async Task<IActionResult> OnPostAddQuote()
    {
        // Only validate NewQuote, not other fields like NewPet  OBS funkar inte med andra fält tomma? Något för AddPet också?
        //ModelState.Remove("FriendInput.NewPet.Name");

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
        //Add new QuoteIM to the FriendInput.Quotes list
        /*var newQuoteIM = new QuoteIM()
        {
            StatusIM = StatusIM.Unchanged,
            QuoteId = Guid.NewGuid(), //Tillfälligt ID, det riktiga skapas i databasen
            QuoteText = FriendInput.NewQuote.QuoteText,
            Author = FriendInput.NewQuote.Author
        };*/
        //FriendInput.Quotes.Add(newQuoteIM);

        //Clear the NewQuote input model
        //FriendInput.NewQuote = new QuoteIM(); 

        await _quotesService.CreateQuoteAsync(quoteDto);


        var friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
        FriendInput = new FriendIM(friend.Item);

        RepopulateCountryselection();

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

        petIM.Name = petIM.editName;

        RepopulateCountryselection();

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
        
        if(quoteIM.StatusIM != StatusIM.Inserted)
        quoteIM.StatusIM = StatusIM.Modified;

        quoteIM.QuoteText = quoteIM.editQuoteText;
        quoteIM.Author = quoteIM.editAuthor;

        RepopulateCountryselection();

        return Page();
    }

    private async Task<IAddress> SaveAddress()
    {
        //Read the existing address from database
        var resp = await _addressesService.ReadAddressAsync(FriendInput.Address.AddressId, false);
        var addressToUpdate = resp.Item;
        //Update the address with the values from the InputModel
        addressToUpdate = FriendInput.Address.UpdateModel(addressToUpdate);
        //Update the model from the InputModel
        var addressToUpdateDto = new AddressCuDto(addressToUpdate);
        //Save the updated address to database
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

        //Jag har valt att inte göra Birthday required eftersom den kan vara null i modellen
        public DateTime? Birthday { get; set; }

        public AddressIM Address { get; set; } = new AddressIM();

        public List<PetIM> Pets { get; set; } = new List<PetIM>();
        public List<QuoteIM> Quotes { get; set; } = new List<QuoteIM>();

        public FriendIM() { } //Ta bort?
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

        [Required(ErrorMessage = "Address must have a street address")]
        public string StreetAddress { get; set; }

        //ZipCode behöver inte vara required, den sätts till 0 om den är null och inget krav att kunna skicka brev
        public int ZipCode { get; set; } = 0;

        [Required(ErrorMessage = "Address must have a city")]
        public string City { get; set; }

        [Required(ErrorMessage = "Address must have a country")]
        public string Country { get; set; }

        public AddressIM() { }
        public AddressIM(AddressIM original)
        {
            StatusIM = StatusIM.Unchanged;
            AddressId = original.AddressId;
            StreetAddress = original.StreetAddress;
            ZipCode = original.ZipCode;
            City = original.City;
            Country = original.Country;
        }
        public AddressIM(IAddress model)
        {
            StatusIM = StatusIM.Unchanged;
            AddressId = model.AddressId;
            StreetAddress = model.StreetAddress;
            ZipCode = model.ZipCode;
            City = model.City;
            Country = model.Country;
        }
        public IAddress UpdateModel(IAddress model)
        {
            model.AddressId = this.AddressId;
            model.StreetAddress = this.StreetAddress;
            model.ZipCode = this.ZipCode;
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
        public string editName { get; set; }

        public PetIM() { }
        public PetIM(PetIM original)
        {
            StatusIM = StatusIM.Unchanged;
            PetId = original.PetId;
            Name = original.Name;

            editName = original.editName;
        }
        public PetIM(IPet model)
        {
            StatusIM = StatusIM.Unchanged;
            PetId = model.PetId;
            Name = editName = model.Name;
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
        public string editQuoteText { get; set; }

        [Required(ErrorMessage = "Quote must have an author")]
        public string editAuthor { get; set; }

        public QuoteIM() { }
        public QuoteIM(QuoteIM original)
        {
            StatusIM = StatusIM.Unchanged;
            QuoteId = original.QuoteId;
            QuoteText = original.QuoteText;
            Author = original.Author;

            editQuoteText = original.editQuoteText;
            editAuthor = original.editAuthor;
        }
        public QuoteIM(IQuote model)
        {
            StatusIM = StatusIM.Unchanged;
            QuoteId = model.QuoteId;
            QuoteText = editQuoteText = model.QuoteText;
            Author = editAuthor = model.Author;
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

    private void RepopulateCountryselection()
    {
        CountrySelection = new SelectList(new List<string>
        {
            "Denmark",
            "Finland",
            "Norway",
            "Sweden",
            "Other",
            "Unknown"
        }, FriendInput.Address.Country);
    }

}
