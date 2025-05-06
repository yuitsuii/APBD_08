using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientsService : IClientsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=apbd;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;";

    public async Task<List<ClientTripDetailsDTO>> GetClientTrips(int clientId)
    {
        var result = new List<ClientTripDetailsDTO>();
        //'using' used for every initiation of connection
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE IdClient = @id", connection);
        checkCmd.Parameters.AddWithValue("@id", clientId);
        var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
        if (!exists)
            throw new Exception("Client not found");
        //query to fetch trips information from given id
        var command = new SqlCommand(@"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
            ct.RegisteredAt, ct.PaymentDate FROM Trip t
            INNER JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip WHERE ct.IdClient = @id", connection);
        command.Parameters.AddWithValue("@id", clientId);
        using var reader = await command.ExecuteReaderAsync();
        //preparing infroamtion for each trip + countries
        while (await reader.ReadAsync())
        {
            result.Add(new ClientTripDetailsDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                PaymentDate = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7)
            });
        }
        return result;
    }
    
    public async Task<int> CreateClient(ClientDTO client)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        //this is a check to ensure I will be commiting changes to the right database
        //turns out I was running sql queries on the master branch(instead of apbd - which I modify) that's why I did not see changes
        //I feel dumb
        
        //var pathCmd = new SqlCommand(@"
        //SELECT physical_name 
        //FROM sys.master_files 
        //WHERE database_id = DB_ID('apbd')", connection);
        //using var reader = await pathCmd.ExecuteReaderAsync();
        //while (await reader.ReadAsync())
        //{
            //Console.WriteLine(reader.GetString(0));
        //}
        //Console.WriteLine(connection.DataSource);
        //Console.WriteLine(connection.Database);

        //regular insert query
        var command = new SqlCommand(@"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)", connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        var newId = (int)await command.ExecuteScalarAsync();
        return newId;
    }
    
    public async Task RegisterClientForTrip(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(); 
        //check for existence
        var checkClientCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE IdClient = @clientId", connection);
        checkClientCmd.Parameters.AddWithValue("@clientId", clientId);
        if ((int)await checkClientCmd.ExecuteScalarAsync() == 0)
            throw new Exception("Client does not exist");
        //check for existence
        var checkTripCmd = new SqlCommand("SELECT COUNT(1) FROM Trip WHERE IdTrip = @tripId", connection); 
        checkTripCmd.Parameters.AddWithValue("@tripId", tripId);
        if ((int)await checkTripCmd.ExecuteScalarAsync() == 0)
            throw new Exception("Trip does not exist");
        //check if a trip is already registered
        var checkExistsCmd = new SqlCommand("SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId", connection);
        checkExistsCmd.Parameters.AddWithValue("@clientId", clientId);
        checkExistsCmd.Parameters.AddWithValue("@tripId", tripId);
        if ((int)await checkExistsCmd.ExecuteScalarAsync() > 0)
            throw new Exception("Client already registered for this trip");
        //validation checks
        var countParticipantsCmd = new SqlCommand(@"SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId", connection); countParticipantsCmd.Parameters.AddWithValue("@tripId", tripId);
        int currentCount = (int)await countParticipantsCmd.ExecuteScalarAsync();
        var maxPeopleCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId", connection);
        maxPeopleCmd.Parameters.AddWithValue("@tripId", tripId);
        int maxPeople = (int)await maxPeopleCmd.ExecuteScalarAsync();
        if (currentCount >= maxPeople)
            throw new Exception("Trip has reached maximum capacity");
        //registry
        var insertCmd = new SqlCommand(@"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@clientId, @tripId, @registeredAt)", connection);
        insertCmd.Parameters.AddWithValue("@clientId", clientId);
        insertCmd.Parameters.AddWithValue("@tripId", tripId);
        var registeredAt = (int)((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(); 
        insertCmd.Parameters.AddWithValue("@registeredAt", registeredAt);
        await insertCmd.ExecuteNonQueryAsync();
    }
    
    
    public async Task UnregisterClientFromTrip(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        //check if registry exists
        var checkCmd = new SqlCommand(@"SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId", connection);
        checkCmd.Parameters.AddWithValue("@clientId", clientId);
        checkCmd.Parameters.AddWithValue("@tripId", tripId);
        if ((int)await checkCmd.ExecuteScalarAsync() == 0)
            throw new Exception("Registration not found");
        //removal of registry itself
        var deleteCmd = new SqlCommand(@"DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId", connection);
        deleteCmd.Parameters.AddWithValue("@clientId", clientId);
        deleteCmd.Parameters.AddWithValue("@tripId", tripId);
        await deleteCmd.ExecuteNonQueryAsync();
    }
}