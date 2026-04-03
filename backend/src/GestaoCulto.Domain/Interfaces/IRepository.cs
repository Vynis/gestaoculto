using System.Collections.Generic;
using System.Threading.Tasks;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Interfaces
{
    public interface IRepository<T> where T : AuditableEntity
    {
        Task<T?> ObterPorIdAsync(long id);
        Task<IReadOnlyList<T>> ListarAsync();
        Task<T> AdicionarAsync(T entidade);
        Task AtualizarAsync(T entidade);
        Task RemoverAsync(T entidade);
    }
}
