using System.Threading.Channels;

namespace WaldauCastle.Services;

/// <summary>Очередь ID заявок для фоновой отправки уведомлений.</summary>
public sealed class BookingNotificationQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<int> Reader => _channel.Reader;

    public bool TryEnqueue(int bookingId) => bookingId > 0 && _channel.Writer.TryWrite(bookingId);
}
