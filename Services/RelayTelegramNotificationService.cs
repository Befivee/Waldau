using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using WaldauCastle.Models;
using WaldauCastle.Options;

namespace WaldauCastle.Services;

/// <summary>Timeweb cannot reach Telegram; forward booking notices to the Hostkey bot.</summary>
public class RelayTelegramNotificationService(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramBotOptions> options,
    ILogger<RelayTelegramNotificationService> logger) : ITelegramNotificationService
{
    public async Task<bool> NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var telegram = options.Value;
        var url = telegram.RelayUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("Telegram RelayUrl пуст — уведомление о заявке #{BookingId} не отправлено.", booking.Id);
            return false;
        }

        try
        {
            var client = httpClientFactory.CreateClient("telegram_relay");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(telegram.RelaySecret))
                request.Headers.TryAddWithoutValidation("X-Relay-Secret", telegram.RelaySecret.Trim());

            request.Content = JsonContent.Create(BookingRelayPayload.From(booking));
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Заявка #{BookingId} передана Telegram-боту через relay {StatusCode}.",
                    booking.Id,
                    (int)response.StatusCode);
                return true;
            }

            logger.LogWarning(
                "Relay отклонил заявку #{BookingId}: {StatusCode}.",
                booking.Id,
                (int)response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Не удалось достучаться до Telegram-релея для заявки #{BookingId}.", booking.Id);
            return false;
        }
    }
}

public sealed class BookingRelayPayload
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("visitDate")]
    public DateTime VisitDate { get; set; }

    [JsonPropertyName("excursionTitle")]
    public string ExcursionTitle { get; set; } = string.Empty;

    [JsonPropertyName("visitTime")]
    public string? VisitTime { get; set; }

    [JsonPropertyName("personsCount")]
    public int PersonsCount { get; set; }

    public static BookingRelayPayload From(Booking booking) => new()
    {
        FullName = booking.FullName,
        Phone = booking.Phone,
        VisitDate = booking.VisitDate,
        ExcursionTitle = booking.ExcursionTitle,
        VisitTime = booking.VisitTime,
        PersonsCount = booking.PersonsCount
    };

    public Booking ToBooking() => new()
    {
        FullName = FullName,
        Phone = Phone,
        VisitDate = VisitDate,
        ExcursionTitle = ExcursionTitle,
        VisitTime = VisitTime,
        PersonsCount = PersonsCount
    };
}
