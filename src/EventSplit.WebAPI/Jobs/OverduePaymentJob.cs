using EventSplit.Application.Services;
using EventSplit.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EventSplit.WebAPI.Jobs;

public class OverduePaymentJob
{
    private readonly IServiceProvider _serviceProvider;

    public OverduePaymentJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<EventService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PaymentHub>>();

        var affectedUserIds = await eventService.MarkOverduePaymentsAsync();

        // Broadcast to all connected clients that overdue payments were marked
        if (affectedUserIds.Count > 0)
        {
            await hubContext.Clients.All.SendAsync("OverduePaymentsMarked", affectedUserIds);
        }
    }
}
