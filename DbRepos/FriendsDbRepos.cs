using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Data;

using Models.Interfaces;
using Models.DTO;
using DbModels;
using DbContext;

namespace DbRepos;

public class FriendsDbRepos
{
    private ILogger<FriendsDbRepos> _logger;
    private readonly MainDbContext _dbContext;

    public FriendsDbRepos(ILogger<FriendsDbRepos> logger, MainDbContext context)
    {
        _logger = logger;
        _dbContext = context;
    }

    public async Task<ResponseItemDto<IFriend>> ReadFriendAsync(Guid id, bool flat)
    {
        IFriend item;
        if (!flat)
        {
            var query = _dbContext.Friends.AsNoTracking()
                .Include(i => i.AddressDbM)
                .Include(i => i.PetsDbM)
                .Include(i => i.QuotesDbM)
                .Where(i => i.FriendId == id);

            item = await query.FirstOrDefaultAsync<IFriend>();
        }
        else
        {
            var query = _dbContext.Friends.AsNoTracking()
                .Where(i => i.FriendId == id);

            item = await query.FirstOrDefaultAsync<IFriend>();
        }
        
        if (item == null) throw new ArgumentException($"Item {id} is not existing");
        return new ResponseItemDto<IFriend>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            Item = item
        };
    }

    public async Task<ResponsePageDto<IFriend>> ReadFriendsAsync(bool seeded, bool flat, string filter, int pageNumber, int pageSize)
    {
        filter ??= "";
        IQueryable<FriendDbM> query;
        if (flat)
        {
            query = _dbContext.Friends.AsNoTracking();
        }
        else
        {
            query = _dbContext.Friends.AsNoTracking()
                .Include(i => i.AddressDbM)
                .Include(i => i.PetsDbM)
                .Include(i => i.QuotesDbM);
        }

        var ret = new ResponsePageDto<IFriend>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            DbItemsCount = await query

            .Where(i => i.Seeded == seeded &&
                        (string.IsNullOrEmpty(filter) || 
                         (filter.ToLower().Contains("unknown") && (i.AddressDbM == null || string.IsNullOrEmpty(i.AddressDbM.Country))) ||
                         (i.AddressDbM != null && !string.IsNullOrEmpty(i.AddressDbM.Country) && filter.ToLower().Contains(i.AddressDbM.Country.ToLower())))).CountAsync(),

            PageItems = await query

            .Where(i => i.Seeded == seeded &&
                        (string.IsNullOrEmpty(filter) || 
                         (filter.ToLower().Contains("unknown") && (i.AddressDbM == null || string.IsNullOrEmpty(i.AddressDbM.Country))) ||
                         (i.AddressDbM != null && !string.IsNullOrEmpty(i.AddressDbM.Country) && filter.ToLower().Contains(i.AddressDbM.Country.ToLower()))))

            .Skip(pageNumber * pageSize)
            .Take(pageSize)

            .ToListAsync<IFriend>(),

            PageNr = pageNumber,
            PageSize = pageSize
        };
        return ret;
    }

    public async Task<ResponseItemDto<IFriend>> DeleteFriendAsync(Guid id)
    {
        var query1 = _dbContext.Friends
            .Where(i => i.FriendId == id);
        var item = await query1.FirstOrDefaultAsync<FriendDbM>();

        if (item == null) throw new ArgumentException($"Item {id} is not existing");

        _dbContext.Friends.Remove(item);

        await _dbContext.SaveChangesAsync();
        return new ResponseItemDto<IFriend>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            Item = item
        };
    }

    public async Task<ResponseItemDto<IFriend>> UpdateFriendAsync(FriendCuDto itemDto)
    {
        var query1 = _dbContext.Friends
            .Where(i => i.FriendId == itemDto.FriendId);
        var item = await query1
            .Include(i => i.AddressDbM)
            .Include(i => i.PetsDbM)
            .Include(i => i.QuotesDbM)
            .FirstOrDefaultAsync<FriendDbM>();

 
        if (item == null) throw new ArgumentException($"Item {itemDto.FriendId} is not existing");

 
        item.UpdateFromDTO(itemDto);

        await navProp_FriendCUdto_to_FriendDbM(itemDto, item);

        _dbContext.Friends.Update(item);

        await _dbContext.SaveChangesAsync();

        return await ReadFriendAsync(item.FriendId, false);
    }

    public async Task<ResponseItemDto<IFriend>> CreateFriendAsync(FriendCuDto itemDto)
    {
        if (itemDto.FriendId != null)
            throw new ArgumentException($"{nameof(itemDto.FriendId)} must be null when creating a new object");


        var item = new FriendDbM(itemDto);

        await navProp_FriendCUdto_to_FriendDbM(itemDto, item);

        _dbContext.Friends.Add(item);

        await _dbContext.SaveChangesAsync();

        return await ReadFriendAsync(item.FriendId, false);
    }

    private async Task navProp_FriendCUdto_to_FriendDbM(FriendCuDto itemDtoSrc, FriendDbM itemDst)
    {
 
        itemDst.AddressDbM = (itemDtoSrc.AddressId != null) ? await _dbContext.Addresses.FirstOrDefaultAsync(
            a => (a.AddressId == itemDtoSrc.AddressId)) : null;

        List<PetDbM> pets = null;
        if (itemDtoSrc.PetsId != null)
        {
            pets = new List<PetDbM>();
            foreach (var id in itemDtoSrc.PetsId)
            {
                var p = await _dbContext.Pets.FirstOrDefaultAsync(i => i.PetId == id);
                if (p == null)
                    throw new ArgumentException($"Item id {id} not existing");

                pets.Add(p);
            }
        }
        itemDst.PetsDbM = pets;

        List<QuoteDbM> quotes = null;
        if (itemDtoSrc.QuotesId != null)
        {
            quotes = new List<QuoteDbM>();
            foreach (var id in itemDtoSrc.QuotesId)
            {
                var q = await _dbContext.Quotes.FirstOrDefaultAsync(i => i.QuoteId == id);
                if (q == null)
                    throw new ArgumentException($"Item id {id} not existing");

                quotes.Add(q);
            }
        }
        itemDst.QuotesDbM = quotes;
    }
}
