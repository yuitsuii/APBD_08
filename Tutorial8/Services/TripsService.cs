using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=apbd;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        //'using' for connection
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        //query to fetch trips information
        var command = new SqlCommand(@"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
        c.IdCountry, c.Name AS CountryName FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
        ORDER BY t.IdTrip", connection);
        using var reader = await command.ExecuteReaderAsync();
        var tripDict = new Dictionary<int, TripDTO>();
        while (await reader.ReadAsync())
        {
            var idTrip = reader.GetInt32(0);
            if (!tripDict.TryGetValue(idTrip, out var trip))
            {
                trip = new TripDTO
                {
                    IdTrip = idTrip,
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<CountryDTO>()
                };
                tripDict[idTrip] = trip;
            }
            if (!reader.IsDBNull(6)) 
            {
                trip.Countries.Add(new CountryDTO
                {
                    IdCountry = reader.GetInt32(6),
                    Name = reader.GetString(7)
                });
            }
        }
        return tripDict.Values.ToList();
    }
}