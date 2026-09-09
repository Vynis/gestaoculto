using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    public class CultoRecorrenciaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string TipoCulto { get; set; } = string.Empty;
        public int DiaSemana { get; set; }
        public string HorarioInicio { get; set; } = string.Empty;
        public string? HorarioFimPrevisto { get; set; }
        public long StatusCultoId { get; set; }
        public string? ObservacoesGerais { get; set; }
        public long? TemplateCultoId { get; set; }
        public int QuantidadeSemanasAntecedencia { get; set; } = 12;
        public bool Ativo { get; set; } = true;
    }

    public class CultoRecorrenciaGeracaoRequest
    {
        public List<string> Datas { get; set; } = new List<string>();
    }

    [ApiController]
    [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
    [Route("api/cultos-recorrencias")]
    public class CultosRecorrenciasController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public CultosRecorrenciasController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lista = await _db.CultosRecorrencias
                .AsNoTracking()
                .OrderByDescending(x => x.Ativo)
                .ThenBy(x => x.DiaSemana)
                .ThenBy(x => x.HorarioInicio)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.TipoCulto,
                    x.DiaSemana,
                    x.HorarioInicio,
                    x.HorarioFimPrevisto,
                    x.StatusCultoId,
                    x.ObservacoesGerais,
                    x.TemplateCultoId,
                    TemplateCultoNome = _db.TemplatesCulto.Where(t => t.Id == x.TemplateCultoId).Select(t => t.Nome).FirstOrDefault(),
                    x.QuantidadeSemanasAntecedencia,
                    x.Ativo,
                    x.UltimaGeracaoEm
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Criar([FromBody] CultoRecorrenciaRequest dto)
        {
            var validacao = await ValidarAsync(dto);
            if (validacao != null)
            {
                return BadRequest(new { mensagem = validacao });
            }

            var entity = new CultoRecorrencia
            {
                Nome = dto.Nome.Trim(),
                TipoCulto = dto.TipoCulto.Trim(),
                DiaSemana = dto.DiaSemana,
                HorarioInicio = ParseHorario(dto.HorarioInicio, "horário de início"),
                HorarioFimPrevisto = ParseHorarioOpcional(dto.HorarioFimPrevisto, "horário de término previsto"),
                StatusCultoId = dto.StatusCultoId,
                ObservacoesGerais = TextoOuNull(dto.ObservacoesGerais),
                TemplateCultoId = dto.TemplateCultoId,
                QuantidadeSemanasAntecedencia = Math.Clamp(dto.QuantidadeSemanasAntecedencia, 1, 52),
                Ativo = dto.Ativo,
                CriadoEm = DateTime.UtcNow
            };

            _db.CultosRecorrencias.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(await ObterRespostaAsync(entity.Id));
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] CultoRecorrenciaRequest dto)
        {
            var entity = await _db.CultosRecorrencias.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Recorrência não encontrada." });
            }

            var validacao = await ValidarAsync(dto);
            if (validacao != null)
            {
                return BadRequest(new { mensagem = validacao });
            }

            entity.Nome = dto.Nome.Trim();
            entity.TipoCulto = dto.TipoCulto.Trim();
            entity.DiaSemana = dto.DiaSemana;
            entity.HorarioInicio = ParseHorario(dto.HorarioInicio, "horário de início");
            entity.HorarioFimPrevisto = ParseHorarioOpcional(dto.HorarioFimPrevisto, "horário de término previsto");
            entity.StatusCultoId = dto.StatusCultoId;
            entity.ObservacoesGerais = TextoOuNull(dto.ObservacoesGerais);
            entity.TemplateCultoId = dto.TemplateCultoId;
            entity.QuantidadeSemanasAntecedencia = Math.Clamp(dto.QuantidadeSemanasAntecedencia, 1, 52);
            entity.Ativo = dto.Ativo;
            entity.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(await ObterRespostaAsync(entity.Id));
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.CultosRecorrencias.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Recorrência não encontrada." });
            }

            var cultosVinculados = await _db.Cultos.Where(x => x.RecorrenciaId == id).ToListAsync();
            foreach (var culto in cultosVinculados)
            {
                culto.RecorrenciaId = null;
                culto.AtualizadoEm = DateTime.UtcNow;
            }

            _db.CultosRecorrencias.Remove(entity);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Recorrência removida com sucesso." });
        }

        [HttpGet("{id:long}/datas-geracao")]
        public async Task<IActionResult> ListarDatasGeracao(long id)
        {
            var recorrencia = await _db.CultosRecorrencias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (recorrencia == null)
            {
                return NotFound(new { mensagem = "Recorrência não encontrada." });
            }

            if (!recorrencia.Ativo)
            {
                return BadRequest(new { mensagem = "Ative a recorrência antes de gerar cultos." });
            }

            var datas = new List<object>();
            foreach (var data in MontarDatas(recorrencia.DiaSemana, recorrencia.QuantidadeSemanasAntecedencia))
            {
                var jaExiste = await _db.Cultos.AnyAsync(c =>
                    c.DataCulto == data.Date &&
                    c.HorarioInicio == recorrencia.HorarioInicio &&
                    (c.RecorrenciaId == recorrencia.Id || c.Nome == recorrencia.Nome));

                datas.Add(new { data = data.ToString("yyyy-MM-dd"), jaExiste });
            }

            return Ok(new { recorrenciaId = recorrencia.Id, nome = recorrencia.Nome, datas });
        }

        [HttpPost("{id:long}/gerar")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Gerar(long id, [FromBody] CultoRecorrenciaGeracaoRequest dto)
        {
            var recorrencia = await _db.CultosRecorrencias.FirstOrDefaultAsync(x => x.Id == id);
            if (recorrencia == null)
            {
                return NotFound(new { mensagem = "Recorrência não encontrada." });
            }

            if (!recorrencia.Ativo)
            {
                return BadRequest(new { mensagem = "Ative a recorrência antes de gerar cultos." });
            }

            if (dto == null || dto.Datas == null || dto.Datas.Count == 0)
            {
                return BadRequest(new { mensagem = "Selecione pelo menos uma data para gerar os cultos." });
            }

            var datasDisponiveis = MontarDatas(recorrencia.DiaSemana, recorrencia.QuantidadeSemanasAntecedencia)
                .Select(x => x.Date)
                .ToHashSet();
            var datasSelecionadas = new List<DateTime>();
            foreach (var dataTexto in dto.Datas)
            {
                if (string.IsNullOrWhiteSpace(dataTexto) ||
                    !DateTime.TryParseExact(dataTexto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ||
                    data.TimeOfDay != TimeSpan.Zero)
                {
                    return BadRequest(new { mensagem = $"Data inválida: {dataTexto}." });
                }

                if (!datasDisponiveis.Contains(data.Date))
                {
                    return BadRequest(new { mensagem = $"A data {dataTexto} não pertence às próximas datas da recorrência." });
                }

                if (datasSelecionadas.Contains(data.Date))
                {
                    return BadRequest(new { mensagem = $"A data {dataTexto} foi selecionada mais de uma vez." });
                }

                datasSelecionadas.Add(data.Date);
            }

            var criados = 0;
            var ignorados = 0;
            var cultoIdsCriados = new List<long>();

            foreach (var data in datasSelecionadas)
            {
                var jaExiste = await _db.Cultos.AnyAsync(c =>
                    c.DataCulto == data.Date &&
                    c.HorarioInicio == recorrencia.HorarioInicio &&
                    (c.RecorrenciaId == recorrencia.Id || c.Nome == recorrencia.Nome));

                if (jaExiste)
                {
                    ignorados += 1;
                    continue;
                }

                var culto = new Culto
                {
                    RecorrenciaId = recorrencia.Id,
                    TemplateCultoId = recorrencia.TemplateCultoId,
                    Nome = recorrencia.Nome,
                    TipoCulto = recorrencia.TipoCulto,
                    DataCulto = data.Date,
                    HorarioInicio = recorrencia.HorarioInicio,
                    HorarioFimPrevisto = recorrencia.HorarioFimPrevisto,
                    StatusCultoId = recorrencia.StatusCultoId,
                    ObservacoesGerais = recorrencia.ObservacoesGerais,
                    CriadoEm = DateTime.UtcNow
                };

                _db.Cultos.Add(culto);
                await _db.SaveChangesAsync();

                if (recorrencia.TemplateCultoId.HasValue)
                {
                    await CriarCronogramaDoTemplateAsync(culto.Id, recorrencia.TemplateCultoId.Value, data.Date);
                }

                cultoIdsCriados.Add(culto.Id);
                criados += 1;
            }

            recorrencia.UltimaGeracaoEm = DateTime.UtcNow;
            recorrencia.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                mensagem = $"{criados} culto(s) criado(s). {ignorados} já existiam e foram ignorados.",
                solicitados = datasSelecionadas.Count,
                criados,
                ignorados,
                cultoIdsCriados
            });
        }

        private async Task<object> ObterRespostaAsync(long id)
        {
            return await _db.CultosRecorrencias
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.TipoCulto,
                    x.DiaSemana,
                    x.HorarioInicio,
                    x.HorarioFimPrevisto,
                    x.StatusCultoId,
                    x.ObservacoesGerais,
                    x.TemplateCultoId,
                    TemplateCultoNome = _db.TemplatesCulto.Where(t => t.Id == x.TemplateCultoId).Select(t => t.Nome).FirstOrDefault(),
                    x.QuantidadeSemanasAntecedencia,
                    x.Ativo,
                    x.UltimaGeracaoEm
                })
                .FirstAsync();
        }

        private async Task<string?> ValidarAsync(CultoRecorrenciaRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
            {
                return "Informe o nome da recorrência.";
            }

            if (string.IsNullOrWhiteSpace(dto.TipoCulto))
            {
                return "Informe o tipo do culto.";
            }

            if (dto.DiaSemana < 0 || dto.DiaSemana > 6)
            {
                return "Informe um dia da semana válido.";
            }

            if (dto.QuantidadeSemanasAntecedencia < 1 || dto.QuantidadeSemanasAntecedencia > 52)
            {
                return "A antecedência deve estar entre 1 e 52 semanas.";
            }

            if (!await _db.StatusCultos.AnyAsync(x => x.Id == dto.StatusCultoId))
            {
                return "Status do culto inválido.";
            }

            if (dto.TemplateCultoId.HasValue && !await _db.TemplatesCulto.AnyAsync(x => x.Id == dto.TemplateCultoId.Value))
            {
                return "Template de culto inválido.";
            }

            ParseHorario(dto.HorarioInicio, "horário de início");
            ParseHorarioOpcional(dto.HorarioFimPrevisto, "horário de término previsto");
            return null;
        }

        private async Task CriarCronogramaDoTemplateAsync(long cultoId, long templateCultoId, DateTime dataCulto)
        {
            if (await _db.EtapasCulto.AnyAsync(x => x.CultoId == cultoId))
            {
                return;
            }

            var etapasTemplate = await _db.TemplatesEtapasCulto
                .AsNoTracking()
                .Include(x => x.AcoesMinisterio)
                .Where(x => x.TemplateCultoId == templateCultoId)
                .OrderBy(x => x.BlocoCronograma)
                .ThenBy(x => x.Sequencia)
                .ToListAsync();

            if (!etapasTemplate.Any())
            {
                return;
            }

            var statusEtapaPadrao = await _db.StatusEtapas.AsNoTracking().OrderBy(x => x.Ordem).Select(x => x.Id).FirstOrDefaultAsync();
            var mapaEtapas = new Dictionary<long, EtapaCulto>();

            foreach (var template in etapasTemplate)
            {
                var horario = dataCulto.Date.Add(template.HorarioInicialPadrao ?? TimeSpan.Zero);
                var etapa = new EtapaCulto
                {
                    CultoId = cultoId,
                    Sequencia = template.Sequencia,
                    HorarioInicio = horario,
                    DuracaoMinutos = template.DuracaoMinutos,
                    HorarioFimCalculado = horario.AddMinutes(Math.Max(1, template.DuracaoMinutos)),
                    Atividade = template.Atividade,
                    BlocoCronograma = string.IsNullOrWhiteSpace(template.BlocoCronograma) ? "PRINCIPAL" : template.BlocoCronograma,
                    Descricao = template.Descricao,
                    MinisterioResponsavelId = template.MinisterioResponsavelId,
                    Observacoes = template.Observacoes,
                    StatusEtapaId = template.StatusEtapaId ?? statusEtapaPadrao,
                    CriadoEm = DateTime.UtcNow
                };

                _db.EtapasCulto.Add(etapa);
                mapaEtapas[template.Id] = etapa;
            }

            await _db.SaveChangesAsync();

            var acoes = etapasTemplate
                .SelectMany(template => template.AcoesMinisterio.Select(acao => new EtapaCultoMinisterioAcao
                {
                    EtapaCultoId = mapaEtapas[template.Id].Id,
                    MinisterioId = acao.MinisterioId,
                    Ordem = acao.Ordem,
                    DescricaoAcao = acao.DescricaoAcao,
                    Observacao = acao.Observacao,
                    Ativo = acao.Ativo,
                    CriadoEm = DateTime.UtcNow
                }))
                .ToList();

            if (acoes.Any())
            {
                _db.EtapasCultoMinisteriosAcoes.AddRange(acoes);
                await _db.SaveChangesAsync();
            }
        }

        private static IReadOnlyList<DateTime> MontarDatas(int diaSemana, int quantidade)
        {
            var hoje = DateTime.Today;
            var diasAteAlvo = (diaSemana - (int)hoje.DayOfWeek + 7) % 7;
            var primeira = hoje.AddDays(diasAteAlvo);
            return Enumerable.Range(0, quantidade).Select(i => primeira.AddDays(i * 7)).ToList();
        }

        private static TimeSpan ParseHorario(string valor, string nomeCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new InvalidOperationException($"Informe o {nomeCampo}.");
            }

            var formatos = new[] { "hh\\:mm", "hh\\:mm\\:ss" };
            if (TimeSpan.TryParseExact(valor, formatos, CultureInfo.InvariantCulture, out var horario) || TimeSpan.TryParse(valor, out horario))
            {
                return horario;
            }

            throw new InvalidOperationException($"{nomeCampo.Substring(0, 1).ToUpper() + nomeCampo.Substring(1)} inválido.");
        }

        private static TimeSpan? ParseHorarioOpcional(string? valor, string nomeCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return ParseHorario(valor, nomeCampo);
        }

        private static string? TextoOuNull(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(texto) ? null : texto;
        }
    }
}
