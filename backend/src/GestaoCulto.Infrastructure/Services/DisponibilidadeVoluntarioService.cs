using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Disponibilidades;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GestaoCulto.Infrastructure.Services
{
    public class DisponibilidadeVoluntarioService : IDisponibilidadeVoluntarioService
    {
        private readonly GestaoCultoDbContext _db;

        public DisponibilidadeVoluntarioService(GestaoCultoDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<CultoDisponibilidadeDto>> ListarCultosFuturosAsync(long voluntarioId, int limite)
        {
            await ValidarVoluntarioAsync(voluntarioId);
            var statusCultoAtivoId = await ObterStatusCultoAtivoIdAsync();
            var hoje = DateTime.Today;
            var quantidade = Math.Max(1, Math.Min(limite, 10));

            var cultos = await _db.Cultos
                .AsNoTracking()
                .Where(x => x.DataCulto >= hoje && x.StatusCultoId == statusCultoAtivoId)
                .OrderBy(x => x.DataCulto)
                .ThenBy(x => x.HorarioInicio)
                .Take(quantidade)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.DataCulto,
                    x.HorarioInicio
                })
                .ToListAsync();

            var cultoIds = cultos.Select(x => x.Id).ToList();
            var disponibilidades = await _db.DisponibilidadesCultoVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId && cultoIds.Contains(x.CultoId))
                .Select(x => new
                {
                    x.CultoId,
                    Codigo = _db.StatusDisponibilidadeVoluntarios
                        .Where(s => s.Id == x.StatusDisponibilidadeId)
                        .Select(s => s.Codigo)
                        .FirstOrDefault(),
                    Nome = _db.StatusDisponibilidadeVoluntarios
                        .Where(s => s.Id == x.StatusDisponibilidadeId)
                        .Select(s => s.Nome)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var cultosEscalados = await _db.Escalas
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId && cultoIds.Contains(x.CultoId))
                .Select(x => x.CultoId)
                .Distinct()
                .ToListAsync();

            return cultos.Select(culto =>
            {
                var disponibilidade = disponibilidades.FirstOrDefault(x => x.CultoId == culto.Id);
                var escalado = cultosEscalados.Contains(culto.Id);
                return new CultoDisponibilidadeDto
                {
                    CultoId = culto.Id,
                    CultoNome = culto.Nome,
                    DataCulto = culto.DataCulto,
                    HorarioInicio = culto.HorarioInicio,
                    StatusCodigo = escalado ? "ESCALADO" : disponibilidade?.Codigo ?? "NAO_RESPONDIDO",
                    StatusNome = escalado ? "Escalado" : disponibilidade?.Nome ?? "Não respondido",
                    Escalado = escalado
                };
            }).ToList();
        }

        public async Task<DisponibilidadeVoluntarioDetalheDto> ObterDetalheAsync(long voluntarioId, long cultoId)
        {
            await ValidarVoluntarioAsync(voluntarioId);
            var statusCultoAtivoId = await ObterStatusCultoAtivoIdAsync();
            var culto = await _db.Cultos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cultoId && x.StatusCultoId == statusCultoAtivoId);

            if (culto == null)
            {
                throw new InvalidOperationException("Culto ativo não encontrado.");
            }

            if (culto.DataCulto.Date < DateTime.Today)
            {
                throw new InvalidOperationException("Não é possível responder disponibilidade para culto passado.");
            }

            var ministerios = await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId)
                .Select(x => new MinisterioDisponibilidadeDto
                {
                    MinisterioId = x.MinisterioId,
                    MinisterioNome = _db.Ministerios
                        .Where(m => m.Id == x.MinisterioId)
                        .Select(m => m.Nome)
                        .FirstOrDefault() ?? string.Empty,
                    Principal = x.Principal
                })
                .OrderByDescending(x => x.Principal)
                .ThenBy(x => x.MinisterioNome)
                .ToListAsync();

            var disponibilidade = await _db.DisponibilidadesCultoVoluntarios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CultoId == cultoId && x.VoluntarioId == voluntarioId);
            var ministerioIds = disponibilidade == null
                ? new List<long>()
                : await _db.DisponibilidadesCultoVoluntariosMinisterios
                    .AsNoTracking()
                    .Where(x => x.DisponibilidadeCultoVoluntarioId == disponibilidade.Id)
                    .Select(x => x.MinisterioId)
                    .ToListAsync();
            var status = disponibilidade == null
                ? null
                : await _db.StatusDisponibilidadeVoluntarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == disponibilidade.StatusDisponibilidadeId);
            var escalado = await _db.Escalas
                .AsNoTracking()
                .AnyAsync(x => x.CultoId == cultoId && x.VoluntarioId == voluntarioId);

            return new DisponibilidadeVoluntarioDetalheDto
            {
                Culto = new CultoDisponibilidadeDto
                {
                    CultoId = culto.Id,
                    CultoNome = culto.Nome,
                    DataCulto = culto.DataCulto,
                    HorarioInicio = culto.HorarioInicio,
                    StatusCodigo = escalado ? "ESCALADO" : status?.Codigo ?? "NAO_RESPONDIDO",
                    StatusNome = escalado ? "Escalado" : status?.Nome ?? "Não respondido",
                    Escalado = escalado
                },
                MinisteriosPermitidos = ministerios,
                MinisterioIdsSelecionados = ministerioIds,
                Observacao = disponibilidade?.Observacao
            };
        }

        public async Task SalvarAsync(
            long voluntarioId,
            long cultoId,
            bool disponivel,
            IReadOnlyCollection<long> ministerioIds,
            string? observacao)
        {
            await ValidarVoluntarioAsync(voluntarioId);
            var statusCultoAtivoId = await ObterStatusCultoAtivoIdAsync();
            var culto = await _db.Cultos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cultoId && x.StatusCultoId == statusCultoAtivoId);

            if (culto == null)
            {
                throw new InvalidOperationException("Culto ativo não encontrado.");
            }

            if (culto.DataCulto.Date < DateTime.Today)
            {
                throw new InvalidOperationException("Não é possível responder disponibilidade para culto passado.");
            }

            var permitidos = await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId)
                .Select(x => x.MinisterioId)
                .ToListAsync();
            var selecionados = disponivel
                ? ministerioIds.Where(x => x > 0).Distinct().ToList()
                : new List<long>();

            if (disponivel && !selecionados.Any())
            {
                throw new InvalidOperationException("Selecione ao menos um ministério para disponibilidade.");
            }

            if (selecionados.Any(x => !permitidos.Contains(x)))
            {
                throw new InvalidOperationException("Foram informados ministérios inválidos para este voluntário.");
            }

            var statusCodigo = disponivel ? "DISPONIVEL" : "INDISPONIVEL";
            var statusId = await _db.StatusDisponibilidadeVoluntarios
                .Where(x => x.Codigo == statusCodigo)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            if (statusId <= 0)
            {
                throw new InvalidOperationException("Status de disponibilidade não encontrado.");
            }

            IDbContextTransaction? transaction = null;
            var controlaTransacao = _db.Database.CurrentTransaction == null;
            if (controlaTransacao)
            {
                transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            }

            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE voluntario SET id = id WHERE id = {0}",
                    voluntarioId);
                var entity = await _db.DisponibilidadesCultoVoluntarios
                    .FirstOrDefaultAsync(x => x.CultoId == cultoId && x.VoluntarioId == voluntarioId);
                var agora = DateTime.UtcNow;

                if (entity == null)
                {
                    entity = new DisponibilidadeCultoVoluntario
                    {
                        CultoId = cultoId,
                        VoluntarioId = voluntarioId,
                        CriadoEm = agora
                    };
                    _db.DisponibilidadesCultoVoluntarios.Add(entity);
                }

                entity.StatusDisponibilidadeId = statusId;
                entity.Observacao = LimparTexto(observacao);
                entity.RespondidoEm = agora;
                entity.AtualizadoEm = agora;
                await _db.SaveChangesAsync();

                var anteriores = await _db.DisponibilidadesCultoVoluntariosMinisterios
                    .Where(x => x.DisponibilidadeCultoVoluntarioId == entity.Id)
                    .ToListAsync();
                _db.DisponibilidadesCultoVoluntariosMinisterios.RemoveRange(anteriores);

                if (disponivel)
                {
                    _db.DisponibilidadesCultoVoluntariosMinisterios.AddRange(selecionados.Select(id =>
                        new DisponibilidadeCultoVoluntarioMinisterio
                        {
                            DisponibilidadeCultoVoluntarioId = entity.Id,
                            MinisterioId = id
                        }));
                }

                await _db.SaveChangesAsync();
                if (controlaTransacao && transaction != null)
                {
                    await transaction.CommitAsync();
                }
            }
            finally
            {
                if (controlaTransacao && transaction != null)
                {
                    transaction.Dispose();
                }
            }
        }

        private async Task ValidarVoluntarioAsync(long voluntarioId)
        {
            if (!await _db.Voluntarios.AsNoTracking().AnyAsync(x => x.Id == voluntarioId && x.Ativo))
            {
                throw new InvalidOperationException("Voluntário ativo não encontrado.");
            }
        }

        private async Task<long> ObterStatusCultoAtivoIdAsync()
        {
            var statusId = await _db.StatusCultos
                .Where(x => x.Codigo == "ATIVO")
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            return statusId > 0
                ? statusId
                : await _db.StatusCultos.OrderBy(x => x.Ordem).Select(x => x.Id).FirstOrDefaultAsync();
        }

        private static string? LimparTexto(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            return texto.Length <= 500 ? texto : texto.Substring(0, 500);
        }
    }
}
