using Telegram.Bot;

namespace WaldauCastle.Services.Telegram;

/// <summary>Через 3 с отправляет «Обрабатываю…» и удаляет сообщение по завершении операции.</summary>
internal sealed class TelegramProcessingIndicator : IAsyncDisposable
{
    private const string ProcessingText = "⏳ Обрабатываю запрос…";
    private static readonly TimeSpan ShowAfter = TimeSpan.FromSeconds(3);

    private readonly ITelegramBotClient _botClient;
    private readonly long _chatId;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly CancellationTokenSource _cancelShowCts;
    private readonly Task<int?> _messageIdTask;

    private TelegramProcessingIndicator(
        ITelegramBotClient botClient,
        long chatId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _botClient = botClient;
        _chatId = chatId;
        _logger = logger;
        _cancellationToken = cancellationToken;
        _cancelShowCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _messageIdTask = ShowAfterDelayAsync(_cancelShowCts.Token);
    }

    public static async Task RunAsync(
        ITelegramBotClient botClient,
        long chatId,
        ILogger logger,
        CancellationToken cancellationToken,
        Func<Task> action)
    {
        await using var indicator = new TelegramProcessingIndicator(botClient, chatId, logger, cancellationToken);
        await action();
    }

    private async Task<int?> ShowAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(ShowAfter, token);
            var message = await _botClient.SendMessage(_chatId, ProcessingText, cancellationToken: token);
            return message.MessageId;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Не удалось показать индикатор обработки в chat {ChatId}.", _chatId);
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cancelShowCts.CancelAsync();
        _cancelShowCts.Dispose();

        int? messageId;
        try
        {
            messageId = await _messageIdTask;
        }
        catch
        {
            return;
        }

        if (messageId is not int id)
            return;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            await _botClient.DeleteMessage(_chatId, id, cancellationToken: cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Не удалось удалить индикатор обработки в chat {ChatId}.", _chatId);
        }
    }
}
