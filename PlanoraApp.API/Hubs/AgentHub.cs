using Microsoft.AspNetCore.SignalR;

namespace PlanoraApp.API.Hubs
{
    // The client will explicitly connect to this Hub to listen for events
    public class AgentHub : Hub
    {
        public async Task JoinTripGroup(string tripId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"trip_{tripId}");
            Console.WriteLine($"[SignalR] Client joined listening group for Trip: {tripId}");
        }

        public async Task LeaveTripGroup(string tripId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip_{tripId}");
        }
        
        // This method can be called by the client to send a chat message
        // to the AI Orchestrator while it is generating the trip.
        public async Task SendMessageToAgent(string tripId, string message)
        {
            // For now, we instantly bounce it back as just a chat acknowledgement
            await Clients.Group($"trip_{tripId}").SendAsync("ReceiveAgentLog", "Received your message, analyzing...");
        }
    }
}
