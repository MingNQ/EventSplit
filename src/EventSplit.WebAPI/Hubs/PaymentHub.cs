using Microsoft.AspNetCore.SignalR;

namespace EventSplit.WebAPI.Hubs;

public class PaymentHub : Hub
{
    public async Task JoinEventGroup(string eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, eventId);
    }

    public async Task LeaveEventGroup(string eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, eventId);
    }
}
