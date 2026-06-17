using System.Threading.Channels;
using WaldauCastle.Models;

namespace WaldauCastle.Services;

/// <summary>Очередь заявок для фоновой отправки уведомлений (не блокирует бота и HTTP-ответ сайта).</summary>
public sealed class BookingNotificationQueue
{
    private readonly Channel<Booking> _channel = Channel.CreateUnbounded<Booking>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<Booking> Reader => _channel.Reader;

    public bool TryEnqueue(Booking booking) => _channel.Writer.TryWrite(booking);
}
