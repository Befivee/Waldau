using System.Net;
using System.Net.Sockets;
using WaldauCastle.Options;

namespace WaldauCastle.Services.Telegram;

internal static class TelegramHttpHandlers
{
    public static SocketsHttpHandler CreateHandler(TelegramBotOptions options)
    {
        var connectTimeout = TimeSpan.FromSeconds(Math.Clamp(options.ConnectTimeoutSeconds, 3, 60));

        if (options.HasProxy)
        {
            return new SocketsHttpHandler
            {
                Proxy = new WebProxy(options.ProxyUrl.Trim()),
                UseProxy = true,
                ConnectTimeout = connectTimeout,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            };
        }

        var preferIpv4 = options.PreferIpv4;

        return new SocketsHttpHandler
        {
            ConnectCallback = (context, cancellationToken) =>
                ConnectDirectAsync(context, preferIpv4, connectTimeout, cancellationToken),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectTimeout = connectTimeout,
        };
    }

    private static async ValueTask<Stream> ConnectDirectAsync(
        SocketsHttpConnectionContext context,
        bool preferIpv4,
        TimeSpan connectTimeout,
        CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;
        var families = preferIpv4
            ? new[] { AddressFamily.InterNetwork }
            : new[] { AddressFamily.InterNetworkV6, AddressFamily.InterNetwork };

        foreach (var family in families)
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
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(connectTimeout);
                    await socket.ConnectAsync(new IPEndPoint(address, port), timeoutCts.Token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                }
            }
        }

        throw new HttpRequestException($"Не удалось подключиться к {host}:{port} (таймаут {connectTimeout.TotalSeconds:0}s).");
    }
}
