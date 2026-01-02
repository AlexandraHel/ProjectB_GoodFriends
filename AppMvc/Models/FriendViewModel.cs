 
 using Models.Interfaces;
using Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models.DTO;
using Services;
using System.ComponentModel.DataAnnotations;

namespace AppMvc.Models
{
    public class FriendViewModel
    {
        public IFriend Friend { get; set; }
      
        public virtual IAddress Address { get; set; } = null;

        public virtual List<IPet> Pets { get; set; } = null;

        public virtual List<IQuote> Quotes { get; set; } = null;


        public SelectList CountrySelection { get; set; }

        [BindProperty]
        public FriendIM FriendInput { get; set; }

        [BindProperty]
        public PetIM NewPetIM { get; set; } = new PetIM();
        
        public SeidoHelpers.ModelValidationResult ValidationResult { get; set; } = new SeidoHelpers.ModelValidationResult(false, null, null);

        public FriendViewModel()
        {
       
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
    }

}