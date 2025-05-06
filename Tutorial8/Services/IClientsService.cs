namespace Tutorial8.Services;
using Tutorial8.Models.DTOs;

public interface IClientsService
{
    Task<List<ClientTripDetailsDTO>> GetClientTrips(int clientId);
    Task<int> CreateClient(ClientDTO client);
    Task RegisterClientForTrip(int clientId, int tripId);
    Task UnregisterClientFromTrip(int clientId, int tripId);
}