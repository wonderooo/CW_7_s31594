using Microsoft.Data.Sqlite;
using VetSqlClient.Models.DTOs;

namespace VetSqlClient.Services;

public interface IDbService
{
    public Task<IEnumerable<ClientGetDTO>> GetClientDetailsAsync();
    public Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync();

}

public class DbService(IConfiguration config) : IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");
    
    public async Task<IEnumerable<ClientGetDTO>> GetClientDetailsAsync()
    {
        var result = new List<ClientGetDTO>();
        
        await using var connection = new SqliteConnection(_connectionString);
        const string sql = "select IdClient, FirstName from Client";
        await using var command = new SqliteCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ClientGetDTO
            {
                Id = reader.GetInt32(0),
                FirstName = reader.GetString(1),
            });
        }

        return result;
    }
    
    public async Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync()
    {
        var result = new Dictionary<int, TripGetDTO>();
        
        await using var connection = new SqliteConnection(_connectionString);
        
        const string sql = @"
        SELECT 
            t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
            c.IdCountry, c.Name
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";
        
        await using var command = new SqliteCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            int idTrip = reader.GetInt32(0);

            if (!result.TryGetValue(idTrip, out var tripDto))
            {
                tripDto = new TripGetDTO
                {
                    Id = idTrip,
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetString(3),
                    DateTo = reader.GetString(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<CountryDTO>()
                };
                result.Add(idTrip, tripDto);
            }

            if (!reader.IsDBNull(6))
            {
                tripDto.Countries.Add(new CountryDTO
                {
                    Id = reader.GetInt32(6),
                    Name = reader.GetString(7)
                });
            }
        }

        return result.Values;
    }
}