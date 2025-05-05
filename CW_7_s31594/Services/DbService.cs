using Microsoft.Data.Sqlite;
using VetSqlClient.Models.DTOs;

namespace VetSqlClient.Services;

public interface IDbService
{
    public Task<ClientGetDTO> GetClientDetailsAsync(int id);
    public Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync();
    public Task<ClientCreateDTO> CreateClientAsync(ClientCreateDTO dto);
    
}

public class DbService(IConfiguration config) : IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");
    
    public async Task<ClientGetDTO> GetClientDetailsAsync(int id)
    {
        var results = new List<ClientGetDTO>();

        await using var connection = new SqliteConnection(_connectionString);
        const string sql = "SELECT IdClient, FirstName, LastName, Email, Telephone, Pesel FROM Client WHERE IdClient = @id";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ClientGetDTO
            {
                Id = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                Telephone = reader.GetString(4),
                Pesel = reader.GetString(5),
            });
        }

        return results[0];
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
    
    public async Task<ClientCreateDTO> CreateClientAsync(ClientCreateDTO dto)
    {
        await using var connection = new SqliteConnection(_connectionString);
        const string sql = "insert into Client (FirstName, LastName, Email, Telephone, Pesel) values (@f, @l, @e, @t, @p);";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@f", dto.FirstName);
        command.Parameters.AddWithValue("@l", dto.LastName);
        command.Parameters.AddWithValue("@e", dto.Email);
        command.Parameters.AddWithValue("@t", dto.Telephone);
        command.Parameters.AddWithValue("@p", dto.Pesel);
        await connection.OpenAsync();
        await command.ExecuteScalarAsync();

        return dto;
    }
}