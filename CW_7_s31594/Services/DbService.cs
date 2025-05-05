using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using VetSqlClient.Models.DTOs;

namespace VetSqlClient.Services;

public interface IDbService
{
    public Task<ClientGetDTO> GetClientDetailsAsync(int id);
    public Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync();
    public Task<ClientCreateDTO> CreateClientAsync(ClientCreateDTO dto);
    public Task<IActionResult> RegisterClientForTripAsync(int clientId, int tripId);
    public Task<IActionResult> UnregisterClientFromTripAsync(int clientId, int tripId);
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
    
    public async Task<IActionResult> RegisterClientForTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var clientExistsCmd = new SqliteCommand("SELECT 1 FROM Client WHERE IdClient = @clientId", connection);
        clientExistsCmd.Parameters.AddWithValue("@clientId", clientId);
        var clientExists = await clientExistsCmd.ExecuteScalarAsync() != null;
        if (!clientExists)
            return new NotFoundObjectResult($"Client with ID {clientId} does not exist.");

        int? maxPeople = null;
        var tripCmd = new SqliteCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId", connection);
        tripCmd.Parameters.AddWithValue("@tripId", tripId);
        await using (var reader = await tripCmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                maxPeople = reader.GetInt32(0);
            }
        }

        if (maxPeople == null)
            return new NotFoundObjectResult($"Trip with ID {tripId} does not exist.");

        var countCmd = new SqliteCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId", connection);
        countCmd.Parameters.AddWithValue("@tripId", tripId);
        var currentCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        if (currentCount >= maxPeople)
            return new BadRequestObjectResult("Maximum number of participants reached.");

        var existsCmd = new SqliteCommand(
            "SELECT 1 FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId", connection);
        existsCmd.Parameters.AddWithValue("@clientId", clientId);
        existsCmd.Parameters.AddWithValue("@tripId", tripId);
        var alreadyRegistered = await existsCmd.ExecuteScalarAsync() != null;

        if (alreadyRegistered)
            return new BadRequestObjectResult("Client is already registered for this trip.");

        var timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var insertCmd = new SqliteCommand(
            "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@clientId, @tripId, @registeredAt)",
            connection);
        insertCmd.Parameters.AddWithValue("@clientId", clientId);
        insertCmd.Parameters.AddWithValue("@tripId", tripId);
        insertCmd.Parameters.AddWithValue("@registeredAt", timestamp);
        await insertCmd.ExecuteNonQueryAsync();

        return new OkObjectResult("Client registered successfully.");
    }
    
    public async Task<IActionResult> UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var checkCmd = new SqliteCommand(
            "SELECT 1 FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId", connection);
        checkCmd.Parameters.AddWithValue("@clientId", clientId);
        checkCmd.Parameters.AddWithValue("@tripId", tripId);
    
        var exists = await checkCmd.ExecuteScalarAsync() != null;
        if (!exists)
            return new NotFoundObjectResult("Registration does not exist.");

        var deleteCmd = new SqliteCommand(
            "DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId", connection);
        deleteCmd.Parameters.AddWithValue("@clientId", clientId);
        deleteCmd.Parameters.AddWithValue("@tripId", tripId);
    
        await deleteCmd.ExecuteNonQueryAsync();

        return new OkObjectResult("Client successfully unregistered from the trip.");
    }


}