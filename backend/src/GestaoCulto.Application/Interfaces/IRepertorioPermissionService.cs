using System.Threading.Tasks;

namespace GestaoCulto.Application.Interfaces
{
    public interface IRepertorioPermissionService
    {
        Task<bool> EhMembroLouvorAsync(long usuarioId);
        Task<bool> PodeGerenciarAsync(long usuarioId, long escalaId);
        Task<long?> ObterEscalaGerenciavelAsync(long usuarioId, long cultoId);
    }
}
