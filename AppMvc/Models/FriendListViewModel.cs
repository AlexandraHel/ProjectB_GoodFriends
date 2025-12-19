using Models.Interfaces;
using Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppMvc.Models
{


    public class FriendListViewModel
    {
    
        public List<IFriend> Friends { get; set; }

        public int NrOfFriends { get; set; }

        public int NrOfPages { get; set; }
        public int PageSize { get; } = 10;

        public int ThisPageNr { get; set; } = 0;
        public int PrevPageNr { get; set; } = 0;
        public int NextPageNr { get; set; } = 0;
        public int NrVisiblePages { get; set; } = 0;

        public string CountryFilter { get; set; } = null;
        public bool Denmark { get; set; }
        
        [BindProperty] //behöver inte vara bindproperty då valet inte finns längre?
        public bool UseSeeds { get; set; } = true; 
        
        [BindProperty]
        public bool Finland { get; set; }
        
        [BindProperty]
        public bool Norway { get; set; }
        
        [BindProperty]
        public bool Sweden { get; set; }
        
        [BindProperty]
        public bool Unknown { get; set; }  //ska den användas på detta sätt eller bara tom?
         
        [BindProperty]        
        public bool Other { get; set; }

        public FriendListViewModel(/*IFriendsService friendsService*/)
        {
            //_friendsService = friendsService;
        }
   
    }
}