using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

using Seido.Utilities.SeedGenerator;
using Models.DTO;
using DbModels;
using DbContext;
using Configuration;
using Encryption;

namespace DbRepos;

public class AdminDbRepos
{
    private const string _seedSource = "./app-seeds.json";
    private readonly ILogger<AdminDbRepos> _logger;
    private Encryptions _encryptions;
    private readonly MainDbContext _dbContext;

    public AdminDbRepos(ILogger<AdminDbRepos> logger, Encryptions encryptions, MainDbContext context)
    {
        _logger = logger;
        _encryptions = encryptions;
        _dbContext = context;
    }

    public async Task<ResponseItemDto<GstUsrInfoAllDto>> InfoAsync() => await DbInfo();


    private async Task<ResponseItemDto<GstUsrInfoAllDto>> DbInfo()
    {
        var info = new GstUsrInfoAllDto();
        info.Db = await _dbContext.InfoDbView.FirstAsync();
        info.Friends = await _dbContext.InfoFriendsView.ToListAsync();
        info.Pets = await _dbContext.InfoPetsView.ToListAsync();
        info.Quotes = await _dbContext.InfoQuotesView.ToListAsync();

        return new ResponseItemDto<GstUsrInfoAllDto>
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif

            Item = info
        };
    }

    public async Task<ResponseItemDto<GstUsrInfoAllDto>> SeedAsync(int nrOfItems)
    {

        //Create a seeder
        var fn = Path.GetFullPath(_seedSource);
        var seeder = new SeedGenerator(fn);

        //Seeding the  quotes table
        var quotes = seeder.AllQuotes.Select(q => new QuoteDbM(q)).ToList();

        #region Full seeding
        //Generate friends and addresses
        var friends = seeder.ItemsToList<FriendDbM>(nrOfItems);
        var addresses = seeder.UniqueItemsToList<AddressDbM>(nrOfItems);

        //Assign Address, Pets and Quotes to all the friends
        foreach (var friend in friends)
        {
            friend.AddressDbM = (seeder.Bool) ? seeder.FromList(addresses) : null;
            friend.PetsDbM = seeder.ItemsToList<PetDbM>(seeder.Next(0, 4));
            friend.QuotesDbM = seeder.UniqueItemsPickedFromList(seeder.Next(0, 6), quotes);
        }

        //Note that all other tables are automatically set through FriendDbM Navigation properties
        _dbContext.Friends.AddRange(friends);
        #endregion

        await _dbContext.SaveChangesAsync();
        return await DbInfo();
    }

    public async Task<ResponseItemDto<GstUsrInfoAllDto>> RemoveSeedAsync(bool seeded)
    {
        // Create parameters based on database provider
        var connection = _dbContext.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;

        List<DbParameter> parameters;
        if (connection is MySqlConnection)
        {
            command.CommandText = "supusr_spDeleteAll";
            parameters = new List<DbParameter>
            {
                new MySqlParameter("seededParam", seeded),
                new MySqlParameter("nrFriendsAffected", MySqlDbType.Int32) { Direction = ParameterDirection.Output },
                new MySqlParameter("nrAddressesAffected", MySqlDbType.Int32) { Direction = ParameterDirection.Output },
                new MySqlParameter("nrPetsAffected", MySqlDbType.Int32) { Direction = ParameterDirection.Output },
                new MySqlParameter("nrQuotesAffected", MySqlDbType.Int32) { Direction = ParameterDirection.Output }
            };
        }
        else if (connection is NpgsqlConnection)
        {
            // PostgreSQL parameters - call as function returning table
            command.CommandText = "SELECT nrFriendsAffected, nrAddressesAffected, nrPetsAffected, nrQuotesAffected FROM supusr.\"spDeleteAll\"(@seededParam)";
            command.CommandType = CommandType.Text;
            parameters =
            [
                new NpgsqlParameter("seededParam", seeded),
                new NpgsqlParameter("nrFriendsAffected", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output },
                new NpgsqlParameter("nrAddressesAffected", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output },
                new NpgsqlParameter("nrPetsAffected", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output },
                new NpgsqlParameter("nrQuotesAffected", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output }
            ];
        }
        else
        {
            // SQL Server parameters (default)
            command.CommandText = "supusr.spDeleteAll";
            parameters = new List<DbParameter>
            {
                new SqlParameter("seededParam", seeded),
                new SqlParameter("nrFriendsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("nrAddressesAffected", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("nrPetsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("nrQuotesAffected", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
        }

        command.Parameters.AddRange(parameters.ToArray());

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        if (connection is NpgsqlConnection)
        {
            // in postgresql, execute a procedure (a function) cannot return a dataset and have output parameters
            // therefore I execute the command without expecting a result set
            await command.ExecuteScalarAsync();
        }
        else
        {
            // Execute the stored procedure and get the result set
            using var reader = await command.ExecuteReaderAsync();

            // map reader result into GstUsrInfoDbDto result_set
            GstUsrInfoDbDto result_set = null;
            if (reader.HasRows)
            {
                // Read the first result set which should be InfoDbView
                await reader.ReadAsync();

                result_set = new GstUsrInfoDbDto
                {
                    // Populate properties from the reader
                    NrSeededFriends = Convert.ToInt32(reader["NrSeededFriends"]),
                    NrUnseededFriends = Convert.ToInt32(reader["NrUnseededFriends"]),
                    NrFriendsWithAddress = Convert.ToInt32(reader["NrFriendsWithAddress"]),
                    NrSeededAddresses = Convert.ToInt32(reader["NrSeededAddresses"]),
                    NrUnseededAddresses = Convert.ToInt32(reader["NrUnseededAddresses"]),
                    NrSeededPets = Convert.ToInt32(reader["NrSeededPets"]),
                    NrUnseededPets = Convert.ToInt32(reader["NrUnseededPets"]),
                    NrSeededQuotes = Convert.ToInt32(reader["NrSeededQuotes"]),
                    NrUnseededQuotes = Convert.ToInt32(reader["NrUnseededQuotes"])
                };
            }
            await reader.CloseAsync();
            // result_set can now be accessed - not used in this example
        }


        // Output parameters can now be accessed - not used in this example
        int nrFriends = (int)parameters.First(p => p.ParameterName == "nrFriendsAffected").Value;
        int nrAddresses = (int)parameters.First(p => p.ParameterName == "nrAddressesAffected").Value;
        int nrPets = (int)parameters.First(p => p.ParameterName == "nrPetsAffected").Value;
        int nrQuotes = (int)parameters.First(p => p.ParameterName == "nrQuotesAffected").Value;

        return await DbInfo();
    }
}
