using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GestaoCulto.Application.Interfaces;

namespace GestaoCulto.Infrastructure.Services
{
    public class ImportacaoMusicaService : IImportacaoMusicaService
    {
        private static readonly Regex VideoIdRegex = new Regex(
            @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlTagRegex = new Regex("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);
        private static readonly Regex VideoSuffixRegex = new Regex(
            @"\s*(?:[-|–—:]\s*)?[\[(].*?(?:official|oficial|video|vídeo|lyric|letra|audio|áudio|visualizer|live|ao vivo|cover|karaoke|4k|hd|remaster).*?[\])]*\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly IHttpClientFactory _httpClientFactory;

        public ImportacaoMusicaService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ImportacaoMusicaResult> ImportarAsync(string linkVideo, CancellationToken cancellationToken = default)
        {
            var videoId = ExtrairVideoId(linkVideo);
            if (videoId == null)
            {
                throw new ArgumentException("Informe um link válido do YouTube.");
            }

            var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
            var metadata = await ObterMetadadosYoutubeAsync(videoUrl, cancellationToken);
            var titulo = LimparTituloYoutube(metadata.Titulo);
            var artista = LimparTexto(metadata.Autor);
            SepararTituloEArtista(ref titulo, ref artista);

            var cifra = await LocalizarCifraAsync(titulo, artista, cancellationToken);
            return new ImportacaoMusicaResult
            {
                Titulo = titulo,
                ArtistaBanda = artista,
                LinkCifra = cifra?.Link,
                LinkVideo = videoUrl,
                Tom = cifra?.Tom,
                Aviso = cifra == null
                    ? "Os dados do YouTube foram encontrados, mas não foi localizada uma cifra correspondente."
                    : cifra.Tom == null
                        ? "A cifra foi localizada, mas o tom precisa ser confirmado."
                        : null
            };
        }

        private async Task<YoutubeMetadata> ObterMetadadosYoutubeAsync(string videoUrl, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("YoutubeMetadata");
            using var response = await client.GetAsync(
                $"oembed?url={Uri.EscapeDataString(videoUrl)}&format=json", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Não foi possível obter os dados do vídeo no YouTube.");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            return new YoutubeMetadata
            {
                Titulo = root.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                Autor = root.TryGetProperty("author_name", out var author) ? author.GetString() ?? string.Empty : string.Empty
            };
        }

        private async Task<CifraMetadata?> LocalizarCifraAsync(string titulo, string artista, CancellationToken cancellationToken)
        {
            var candidatos = GerarCandidatos(titulo, artista);

            foreach (var candidato in candidatos.Where(x => !string.IsNullOrWhiteSpace(x.Titulo) && !string.IsNullOrWhiteSpace(x.Artista)))
            {
                var artistaSlug = GerarSlug(candidato.Artista);
                var musicaSlug = GerarSlug(candidato.Titulo);
                var link = $"https://www.cifraclub.com.br/{artistaSlug}/{musicaSlug}/";
                var client = _httpClientFactory.CreateClient("CifraClub");
                using var response = await client.GetAsync(link, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync();
                // O Cifra Club alterou o HTML e deixou de usar o antigo "cifra_cnt".
                if (html.IndexOf("data-chord-select", StringComparison.OrdinalIgnoreCase) < 0
                    && html.IndexOf("data-chords-list", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var tom = ExtrairTom(html);
                var linkFinal = response.RequestMessage?.RequestUri?.ToString() ?? link;
                return new CifraMetadata { Link = linkFinal, Tom = tom };
            }

            return null;
        }

        private static string? ExtrairVideoId(string link)
        {
            if (!Uri.TryCreate(link?.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || !uri.Host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase)
                    && !uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return VideoIdRegex.Match(uri.ToString()).Groups[1].Value is var id && !string.IsNullOrWhiteSpace(id)
                ? id
                : null;
        }

        private static void SepararTituloEArtista(ref string titulo, ref string artista)
        {
            var separador = EncontrarSeparador(titulo);
            if (separador == null)
            {
                return;
            }

            var partes = titulo.Split(new[] { separador }, 2, StringSplitOptions.None);
            var primeiraParte = partes[0].Trim();
            var segundaParte = partes[1].Trim();
            if (string.Equals(GerarSlug(primeiraParte), GerarSlug(artista), StringComparison.OrdinalIgnoreCase))
            {
                titulo = segundaParte;
                return;
            }

            if (string.IsNullOrWhiteSpace(artista))
            {
                titulo = primeiraParte;
                artista = segundaParte;
            }
        }

        private static string? EncontrarSeparador(string texto)
        {
            foreach (var separador in new[] { " - ", " | ", " – ", " — ", " : " })
            {
                if (texto.Contains(separador, StringComparison.Ordinal))
                {
                    return separador;
                }
            }

            return null;
        }

        private static string LimparTituloYoutube(string texto)
        {
            var titulo = LimparTexto(texto);
            titulo = VideoSuffixRegex.Replace(titulo, string.Empty).Trim();
            titulo = Regex.Replace(titulo, @"\s+(?:official|oficial|video|vídeo|lyric|lyrics|letra|audio|áudio|visualizer|4k|hd)\s*$", string.Empty, RegexOptions.IgnoreCase);
            return titulo.Trim(' ', '-', '|', '–', '—', ':');
        }

        private static (string Titulo, string Artista)[] GerarCandidatos(string titulo, string artista)
        {
            var tituloLimpo = LimparTituloYoutube(titulo);
            var candidatos = new[]
            {
                (Titulo: tituloLimpo, Artista: artista),
                (Titulo: RemoverSufixoVersao(tituloLimpo), Artista: artista)
            };

            var separador = EncontrarSeparador(tituloLimpo);
            if (separador != null)
            {
                var partes = tituloLimpo.Split(new[] { separador }, 2, StringSplitOptions.None);
                candidatos = candidatos.Concat(new[]
                {
                    (Titulo: partes[1].Trim(), Artista: partes[0].Trim()),
                    (Titulo: partes[0].Trim(), Artista: partes[1].Trim())
                }).ToArray();
            }

            return candidatos
                .Where(x => !string.IsNullOrWhiteSpace(x.Titulo) && !string.IsNullOrWhiteSpace(x.Artista))
                .GroupBy(x => $"{GerarSlug(x.Artista)}/{GerarSlug(x.Titulo)}")
                .Select(x => x.First())
                .ToArray();
        }

        private static string LimparTexto(string texto)
        {
            return WhitespaceRegex.Replace(HtmlTagRegex.Replace(texto ?? string.Empty, " "), " ").Trim();
        }

        private static string RemoverSufixoVersao(string texto)
        {
            return Regex.Replace(texto, @"\s*[\[(].*?(cover|acoustic|live|ao vivo|karaoke|official|oficial|video|vídeo|lyric|lyrics).*?[\])]*\s*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        private static string GerarSlug(string texto)
        {
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var semAcentos = new string(normalizado.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            return Regex.Replace(semAcentos.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        }

        private static string? ExtrairTom(string html)
        {
            var texto = WhitespaceRegex.Replace(HtmlTagRegex.Replace(html, " "), " ");
            var match = Regex.Match(texto, @"\bTom\s*[:\-]?\s*([A-G](?:#|b)?(?:m|maj|min|sus|dim)?[0-9]?)\b", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private sealed class YoutubeMetadata
        {
            public string Titulo { get; set; } = string.Empty;
            public string Autor { get; set; } = string.Empty;
        }

        private sealed class CifraMetadata
        {
            public string Link { get; set; } = string.Empty;
            public string? Tom { get; set; }
        }
    }
}
