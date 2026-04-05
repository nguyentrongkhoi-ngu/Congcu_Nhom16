using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CinemaBooking.Hubs
{
    public class BookingHub : Hub
    {
        // Simple in-memory store for "Currently Selecting" seats
        // Key: ScreeningId (MaLichChieu), Value: Dictionary<SeatId, ConnectionId>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> SelectingSeats = 
            new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

        public async Task JoinScreening(string screeningId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, screeningId);
            
            // Send current selecting status to the newcomer
            if (SelectingSeats.TryGetValue(screeningId, out var seats))
            {
                var selectedByOthers = seats.Select(x => x.Key).ToList();
                await Clients.Caller.SendAsync("UpdateSelectingSeats", selectedByOthers);
            }
        }

        public async Task SelectSeat(string screeningId, string seatId)
        {
            var screeningSelection = SelectingSeats.GetOrAdd(screeningId, _ => new ConcurrentDictionary<string, string>());
            
            if (screeningSelection.TryAdd(seatId, Context.ConnectionId))
            {
                await Clients.GroupExcept(screeningId, Context.ConnectionId).SendAsync("SeatSelectedByOther", seatId);
            }
        }

        public async Task DeselectSeat(string screeningId, string seatId)
        {
            if (SelectingSeats.TryGetValue(screeningId, out var screeningSelection))
            {
                if (screeningSelection.TryRemove(seatId, out _))
                {
                    await Clients.GroupExcept(screeningId, Context.ConnectionId).SendAsync("SeatDeselectedByOther", seatId);
                }
            }
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Clean up all seats selected by this connection
            foreach (var screening in SelectingSeats)
            {
                var seatsToRemove = screening.Value.Where(x => x.Value == Context.ConnectionId).Select(x => x.Key).ToList();
                foreach (var seatId in seatsToRemove)
                {
                    if (screening.Value.TryRemove(seatId, out _))
                    {
                        await Clients.Group(screening.Key).SendAsync("SeatDeselectedByOther", seatId);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
