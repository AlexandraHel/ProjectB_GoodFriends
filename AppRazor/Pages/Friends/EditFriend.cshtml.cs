using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppRazor.SeidoHelpers;
using Services.Interfaces;
using Models.Interfaces;
using Models.DTO;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Models;
using System.Net.WebSockets;

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
        public async Task<IActionResult> OnGet()
        {
            StatusIM statusIM;
            Guid _friendId = Guid.Parse(Request.Query["id"]);
            
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
            var pet = FriendInput.Pets.FirstOrDefault(a => a.PetId == petId);
            if (pet != null)
            {
                pet.StatusIM = StatusIM.Deleted;
            }

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
            //Ta bort knappar om detta inte ska användas
        
            string[] keys = { "FriendInput.NewPet.Name"};

            if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }
            //ModelState.Remove("FriendInput.NewQuote.QuoteText");
            //ModelState.Remove("FriendInput.NewQuote.Author");
            
            if (ModelState.IsValid)
            {
                //Add new PetIM to the FriendInput.Pets list
                var newPetIM = new PetIM()
                {
                    StatusIM = StatusIM.Unchanged,
                    PetId = Guid.NewGuid(),
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
            // Only validate NewQuote, not other fields like NewPet
            ModelState.Remove("FriendInput.NewPet.Name");
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                // Set breakpoint here or log errors
            }
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
            string[] keys = { "FriendInput.FirstName",
                              "FriendInput.LastName", 
                              "FriendInput.Email"};
            if (!ModelState.IsValidPartially(out SeidoHelpers.ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }
            var resp = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
            var friendToUpdate = resp.Item;
            //await SavePets();
            //await SaveQuotes();

               //Update the friend with the values from the InputModel
                friendToUpdate = FriendInput.UpdateModel(friendToUpdate);
                var friendToUpdateDto = new FriendCuDto(friendToUpdate);
                //Save the updated friend to database
                await _friendsService.UpdateFriendAsync(friendToUpdateDto);

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

               return Redirect($"~/Friends/ListOfFriends");
            
/*
            var friend = FriendInput.UpdateModel(friend);
            await _mg_service.UpdateMusicGroupAsync(new MusicGroupCUdto(mg));

            if (MusicGroupInput.StatusIM == StatusIM.Inserted)
            {
                return Redirect($"~/ListOfGroups");
            }

            return Redirect($"~/ViewFriend?id={FriendInput.FriendId}");*/
            
           /* if (ModelState.IsValid)
            {
                //Read the existing friend from database
                var resp = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);
                var friendToUpdate = resp.Item;

                //Update the friend with the values from the InputModel
                friendToUpdate = FriendInput.UpdateModel(friendToUpdate);
                var friendToUpdateDto = new FriendCuDto(friendToUpdate);
                //Save the updated friend to database
                await _friendsService.UpdateFriendAsync(friendToUpdateDto);

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
            return Page();*/
        }

        public async Task<IActionResult> OnPostUndo()
        {
            var friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);

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

            [Required(ErrorMessage = "Your friend must have an email")]
            public string Email { get; set; }
            public DateTime? Birthday { get; set; }
            public string StreetAddress { get; set; } = "" ;
            public int ZipCode { get; set; } = 0;
            public string City { get; set; } = "" ;
            public string Country { get; set; } = "" ; // Ska andra länder tillåtas? Selectlist/dropdown?

            public List<PetIM> Pets { get; set; } = new List<PetIM>();
            public List<QuoteIM> Quotes { get; set; } = new List<QuoteIM>();

            public FriendIM() {}
            public FriendIM(IFriend model) 
            {
                StatusIM = StatusIM.Unchanged;
                FriendId = model.FriendId;
                FirstName = model.FirstName;
                LastName = model.LastName;
                Email = model.Email;
                Birthday = model.Birthday;
                StreetAddress = model.Address?.StreetAddress ?? "";
                ZipCode = model.Address?.ZipCode ?? 0;
                City = model.Address?.City ?? "";
                Country = model.Address?.Country ?? "";

                Pets = model.Pets?.Select(m => new PetIM(m)).ToList() ?? new List<PetIM>();
                Quotes = model.Quotes?.Select(m => new QuoteIM(m)).ToList() ?? new List<QuoteIM>();
            }

            public IFriend UpdateModel(IFriend model)
            {
                model.FirstName = this.FirstName;
                model.LastName = this.LastName;
                model.Email = this.Email;
                model.Birthday = this.Birthday;

                if (model.Address != null)
                {
                    model.Address.StreetAddress = this.StreetAddress;
                    model.Address.ZipCode = this.ZipCode;
                    model.Address.City = this.City;
                    model.Address.Country = this.Country;
                }

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

            public PetCuDto CreateCUdto () => new PetCuDto()
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
/*
    <button type="button" class="btn btn-danger btn-sm m-1"
                                    data-seido-selected-item-id="@Model.FriendInput.Quotes[k].QuoteId"
                                    data-bs-toggle="modal" data-bs-target="#dangerDelQModal"

                                    data-seido-modal-title ="Delete Quote"
                                    data-seido-modal-body="@Model.FriendInput.Quotes[k].QuoteText is about to be deleted.">
                                    Del
                                </button>

                                 <div class="modal fade" id="dangerDelQModal" tabindex="-1" aria-labelledby="softModalLabel" aria-hidden="true">
                                    <div class="modal-dialog">
                                        <div class="modal-content">
                                            <div class="modal-header">
                                                <h5 class="modal-title text-danger" id="softModalLabel">Confirm deletion</h5>
                                                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                                            </div>
                                            <div class="modal-body">
                                                Quote will be deleted.
                                            </div>
                                            <div class="modal-footer">
                                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                                <button type="submit" asp-page-handler="DeleteQuote"  data-seido-selected-item-id="@Model.FriendInput.Quotes[k].QuoteId" class="btn btn-primary btn-danger" data-bs-dismiss="modal">Ok</button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                        </div> */