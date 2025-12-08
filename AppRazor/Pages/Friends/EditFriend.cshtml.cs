using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Models.Interfaces;
using Models.DTO;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AppRazor.Pages.Friends;

public class EditFriendModel: PageModel
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

        [BindProperty]
        public FriendIM FriendInput { get; set; }

          //I also use BindProperty to keep between several posts, bound to hidden <input> field
        //[BindProperty]
        //public string PageHeader { get; set; } //Behövs denna?
        //public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, null, null);

        public EditFriendModel(IFriendsService friendsService, IAddressesService addressesService, IPetsService petsService, IQuotesService quotesService)
        {
        _friendsService = friendsService;
        _addressesService = addressesService;
        _petsService = petsService;
        _quotesService = quotesService;
        }

        public async Task<IActionResult> OnGet(string id)
        {
            Guid _friendId = Guid.Parse(id);
            var response = await _friendsService.ReadFriendAsync(_friendId, false);
            FriendInput = new FriendIM(response.Item);
            /*Friend = response.Item;
        
                Pets = Friend.Pets?.ToList();
                Quotes = Friend.Quotes?.ToList();
                Address = Friend.Address;*/

            return Page();
        }
            public IActionResult OnPostDeletePet(Guid petId)
        {
            //Set the Artist as deleted, it will not be rendered
            FriendInput.Pets.First(a => a.PetId == petId).StatusIM = StatusIM.Deleted;

            return Page();
        }
            public IActionResult OnPostDeleteQuote(Guid quoteId)
        {
            //Set the Artist as deleted, it will not be rendered
            FriendInput.Quotes.First(a => a.QuoteId == quoteId).StatusIM = StatusIM.Deleted;

            return Page();
        }

        public IActionResult OnPostAddPet()
        {
            if (ModelState.IsValid)
            {
                //Add new PetIM to the FriendInput.Pets list
                var newPetIM = new PetIM()
                {
                    StatusIM = StatusIM.Unchanged,
                    PetId = Guid.NewGuid(), //Temporary Guid, will be replaced when saved to database
                    Name = FriendInput.NewPet.Name
                };
                FriendInput.Pets.Add(newPetIM);

                //Clear the NewPet input model
                FriendInput.NewPet = new PetIM();
            }

            return Page();
        }
        public IActionResult OnPostAddQuote()
        {
            if (ModelState.IsValid)
            {
                //Add new QuoteIM to the FriendInput.Quotes list
                var newQuoteIM = new QuoteIM()
                {
                    StatusIM = StatusIM.Unchanged,
                    QuoteId = Guid.NewGuid(), //Temporary Guid, will be replaced when saved to database
                    QuoteText = FriendInput.NewQuote.QuoteText,
                    Author = FriendInput.NewQuote.Author
                };
                FriendInput.Quotes.Add(newQuoteIM);

                //Clear the NewQuote input model
                FriendInput.NewQuote = new QuoteIM();
            }

            return Page();
        }

        public IActionResult OnPostEditPet(Guid petId)
        {
            //Find the PetIM to edit
            var petIM = FriendInput.Pets.First(p => p.PetId == petId);
            //Set its status to Modified so it will be updated in database
            petIM.StatusIM = StatusIM.Modified;

            return Page();
        }
         public IActionResult OnPostEditQuote(Guid quoteId)
        {
                //Find the QuoteIM to edit
                var quoteIM = FriendInput.Quotes.First(q => q.QuoteId == quoteId);
                //Set its status to Modified so it will be updated in database
                quoteIM.StatusIM = StatusIM.Modified;
    
                return Page();
        }

        public async Task<IActionResult> OnPostSave()
        {
            if (ModelState.IsValid)
            {
                //Read the existing friend from database
                var resp = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
                var friendToUpdate = resp.Item;

                //Update the friend with the values from the InputModel
                friendToUpdate = FriendInput.UpdateModel(friendToUpdate);

                //Save the updated friend to database
                await _friendsService.UpdateFriendAsync(friendToUpdate);

                //Handle Pets
                foreach (var petIM in FriendInput.Pets)
                {
                    if (petIM.StatusIM == StatusIM.Deleted)
                    {
                        await _petsService.DeletePetAsync(petIM.PetId);
                    }
                }

                //Handle Quotes
                foreach (var quoteIM in FriendInput.Quotes)
                {
                    if (quoteIM.StatusIM == StatusIM.Deleted)
                    {
                        await _quotesService.DeleteQuoteAsync(quoteIM.QuoteId);
                    }
                }

               return Redirect($"~/Friends/Overview");
            }
            return Page();
        }

               public async Task<IActionResult> OnPostUndo()
        {
            //Reload Music group from Database
            var friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);

            //Repopulate the InputModel
            FriendInput = new FriendIM(friend.Item);
            return Page();
        }
        #region Input Models
        public enum StatusIM { Unknown, Unchanged, Modified, Deleted}
        public class FriendIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid FriendId { get; set; }

            [Required(ErrorMessage = "Your friend must have a firstname")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Your friend must have a lastname")]
            public string LastName { get; set; }

            public int StreetAddress { get; set; }
            public int ZipCode { get; set; }
            public string City { get; set; }
            public string Country { get; set; } // Ska andra länder tillåtas? Selectlist?

            /*Made nullable (and required) to force user to make an active selection when creating new group
            public MusicGenre? Genre { get; set; }*/

            public List<PetIM> Pets { get; set; } = new List<PetIM>();
            public List<QuoteIM> Quotes { get; set; } = new List<QuoteIM>();

            public FriendIM() {}
            public FriendIM(IFriend model)  // Ska inte kunna skapa en ny friend så den behövs nog inte
            {
                StatusIM = StatusIM.Unchanged;
                FriendId = model.FriendId;
                FirstName = model.FirstName;
                LastName = model.LastName;
                StreetAddress = model.StreetAddress;
                ZipCode = model.ZipCode;
                City = model.City;
                Country = model.Country;

                Pets = model.Pets?.Select(m => new PetIM(m)).ToList();
                Quotes = model.Quotes?.Select(m => new QuoteIM(m)).ToList();
            }

            //to update the model in database
            public IFriend UpdateModel(IFriend model)
            {
                model.FirstName = this.FirstName;
                model.LastName = this.LastName;
                model.StreetAddress = this.StreetAddress;
                model.ZipCode = this.ZipCode;
                model.City = this.City;
                model.Country = this.Country;
                return model;
            }

            public PetIM NewPet { get; set; } = new PetIM();
            public QuoteIM NewQuote { get; set; } = new QuoteIM();


        }

        public class PetIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid PetId { get; set; }

            [Required(ErrorMessage = "Pet must have a name")]
            public string Name { get; set; }

            public PetIM() { }
            public PetIM(PetIM original)
            {
                StatusIM = StatusIM.Unchanged;
                PetId = original.PetId;
                Name = original.Name;
            }
            public PetIM(IPet model)
            {
                StatusIM = StatusIM.Unchanged;
                PetId = model.PetId;
                Name = model.Name;
            }
               public IPet UpdateModel(IPet model)
            {
                model.PetId = this.PetId;
                model.Name = this.Name;
                return model;
            }

            //to create new artist in the database
            public PetCuDto CreateCUdto () => new PetCuDto(){

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

            [Required(ErrorMessage = "Quote must have author")]
            public string Author { get; set; }

            public QuoteIM() { }
            public QuoteIM(QuoteIM original)
            {
                StatusIM = StatusIM.Unchanged;
                QuoteId = original.QuoteId;
                QuoteText = original.QuoteText;
                Author = original.Author;
            }
            public QuoteIM(IQuote model)
            {
                StatusIM = StatusIM.Unchanged;
                QuoteId = model.QuoteId;
                QuoteText = model.QuoteText;
                Author = model.Author;
            }
             public IQuote UpdateModel(IQuote model)
            {
                model.QuoteId = this.QuoteId;
                model.QuoteText = this.QuoteText;
                model.Author = this.Author;
                return model;
            }
               public QuoteCuDto CreateCUdto () => new QuoteCuDto(){
                QuoteId = null,
                Quote = this.QuoteText,
                Author = this.Author
            };
        }
        #endregion
}