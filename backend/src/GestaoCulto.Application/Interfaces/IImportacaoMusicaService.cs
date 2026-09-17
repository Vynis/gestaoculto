using System.Threading;
using System.Threading.Tasks;

namespace GestaoCulto.Application.Interfaces
{
    public class ImportacaoMusicaResult
    {
        public string Titulo { get; set; } = string.Empty;
        public string ArtistaBanda { get; set; } = string.Empty;
        public string? Tom { get; set; }
        public string? LinkCifra { get; set; }
        public string LinkVideo { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
        public string? Aviso { get; set; }
    }

    public interface IImportacaoMusicaService
    {
        Task<ImportacaoMusicaResult> ImportarAsync(string linkVideo, CancellationToken cancellationToken = default);
    }
}
