using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Cultos;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.Infrastructure.Services
{
    public class CultoService : ICultoService
    {
        private readonly GestaoCultoDbContext _db;

        public CultoService(GestaoCultoDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<CultoResponseDto>> ListarAsync()
        {
            return await _db.Cultos
                .AsNoTracking()
                .OrderByDescending(c => c.DataCulto)
                .ThenBy(c => c.HorarioInicio)
                .Select(c => new CultoResponseDto
                {
                    Id = c.Id,
                    RecorrenciaId = c.RecorrenciaId,
                    Nome = c.Nome,
                    TipoCulto = c.TipoCulto,
                    DataCulto = c.DataCulto,
                    HorarioInicio = c.HorarioInicio,
                    HorarioFimPrevisto = c.HorarioFimPrevisto,
                    StatusCultoId = c.StatusCultoId,
                    ObservacoesGerais = c.ObservacoesGerais,
                    TotalVisitantes = c.TotalVisitantes,
                    TotalNovosConvertidos = c.TotalNovosConvertidos
                })
                .ToListAsync();
        }

        public async Task<CultoResponseDto?> ObterPorIdAsync(long id)
        {
            return await _db.Cultos
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CultoResponseDto
                {
                    Id = c.Id,
                    RecorrenciaId = c.RecorrenciaId,
                    Nome = c.Nome,
                    TipoCulto = c.TipoCulto,
                    DataCulto = c.DataCulto,
                    HorarioInicio = c.HorarioInicio,
                    HorarioFimPrevisto = c.HorarioFimPrevisto,
                    StatusCultoId = c.StatusCultoId,
                    ObservacoesGerais = c.ObservacoesGerais,
                    TotalVisitantes = c.TotalVisitantes,
                    TotalNovosConvertidos = c.TotalNovosConvertidos
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CultoResponseDto> CriarAsync(CultoRequestDto dto)
        {
            var dataCulto = ParseData(dto.DataCulto);
            var horarioInicio = ParseHorario(dto.HorarioInicio, "horário de início");
            var horarioFimPrevisto = ParseHorarioOpcional(dto.HorarioFimPrevisto, "horário de término previsto");

            var entity = new Culto
            {
                Nome = dto.Nome,
                TipoCulto = dto.TipoCulto,
                DataCulto = dataCulto.Date,
                HorarioInicio = horarioInicio,
                HorarioFimPrevisto = horarioFimPrevisto,
                StatusCultoId = dto.StatusCultoId,
                ObservacoesGerais = dto.ObservacoesGerais,
                TemplateCultoId = dto.TemplateCultoId,
                CriadoEm = System.DateTime.UtcNow
            };

            _db.Cultos.Add(entity);
            await _db.SaveChangesAsync();

            return (await ObterPorIdAsync(entity.Id))!;
        }

        public async Task<CultoResponseDto?> AtualizarAsync(long id, CultoRequestDto dto)
        {
            var entity = await _db.Cultos.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }

            var dataCulto = ParseData(dto.DataCulto);
            var horarioInicio = ParseHorario(dto.HorarioInicio, "horário de início");
            var horarioFimPrevisto = ParseHorarioOpcional(dto.HorarioFimPrevisto, "horário de término previsto");

            entity.Nome = dto.Nome;
            entity.TipoCulto = dto.TipoCulto;
            entity.DataCulto = dataCulto.Date;
            entity.HorarioInicio = horarioInicio;
            entity.HorarioFimPrevisto = horarioFimPrevisto;
            entity.StatusCultoId = dto.StatusCultoId;
            entity.ObservacoesGerais = dto.ObservacoesGerais;
            entity.TemplateCultoId = dto.TemplateCultoId;
            entity.AtualizadoEm = System.DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return await ObterPorIdAsync(entity.Id);
        }

        public async Task<bool> ExcluirAsync(long id)
        {
            var entity = await _db.Cultos.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return false;
            }

            _db.Cultos.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        private static DateTime ParseData(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new InvalidOperationException("Informe a data do culto.");
            }

            var formatos = new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff", "dd/MM/yyyy" };
            if (DateTime.TryParseExact(valor, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            {
                return data;
            }

            if (DateTime.TryParse(valor, out data))
            {
                return data;
            }

            throw new InvalidOperationException("Data do culto inválida.");
        }

        private static TimeSpan ParseHorario(string valor, string nomeCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new InvalidOperationException($"Informe o {nomeCampo}.");
            }

            var formatos = new[] { "hh\\:mm", "hh\\:mm\\:ss" };
            if (TimeSpan.TryParseExact(valor, formatos, CultureInfo.InvariantCulture, out var horario))
            {
                return horario;
            }

            if (TimeSpan.TryParse(valor, out horario))
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
    }
}
