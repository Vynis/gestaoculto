using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Telegram;

namespace GestaoCulto.Application.Interfaces
{
    public interface ITelegramService
    {
        Task<TelegramVinculoDto> GerarVinculoAsync(long voluntarioId);
        Task<TelegramConexaoStatusDto> ObterStatusAsync(long voluntarioId);
        Task DesvincularAsync(long voluntarioId);
        Task ProcessarUpdateAsync(TelegramUpdateDto update);
        Task ConfigurarWebhookAsync();
    }
}
