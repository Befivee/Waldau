using System.Net;
using System.Net.Sockets;

namespace WaldauCastle.Services.Telegram;

internal static class TelegramHttpHandlers
{
    public static SocketsHttpHandler CreateIpv6PreferredHandler() => new()
    {
        ConnectCallback = ConnectIpv6PreferredAsync,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    };

    private static async ValueTask<Stream> ConnectIpv6PreferredAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        foreach (var family in new[] { AddressFamily.InterNetworkV6, AddressFamily.InterNetwork })
        {
            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, family, cancellationToken);
            }
            catch
            {
                continue;
            }

            foreach (var address in addresses)
            {
                var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, port), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                }
            }
        }

        throw new HttpRequestException($"Не удалось подключиться к {host}:{port} (IPv6/IPv4).");
    }
}
