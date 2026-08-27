using System.Collections.Generic;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Telegram;

namespace GestaoCulto.Application.Interfaces
{
    public interface ITelegramBotClient
    {
        Task EnviarMensagemAsync(long chatId, string texto, IReadOnlyCollection<TelegramBotaoDto>? botoes = null);
        Task EditarMensagemAsync(long chatId, long messageId, string texto, IReadOnlyCollection<TelegramBotaoDto>? botoes = null);
        Task ResponderCallbackAsync(string callbackId, string? texto = null);
        Task ConfigurarWebhookAsync();
    }
}
