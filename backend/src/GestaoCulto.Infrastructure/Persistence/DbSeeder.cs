using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.Infrastructure.Persistence
{
    public class DbSeeder
    {
        private readonly GestaoCultoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;

        public DbSeeder(GestaoCultoDbContext db, IPasswordHasher passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync()
        {
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ministerio_lider (
                  ministerio_id BIGINT UNSIGNED NOT NULL,
                  usuario_id BIGINT UNSIGNED NOT NULL,
                  principal TINYINT(1) NOT NULL DEFAULT 0,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (ministerio_id, usuario_id),
                  CONSTRAINT fk_ministerio_lider_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE CASCADE,
                  CONSTRAINT fk_ministerio_lider_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            if (!await ColunaExisteAsync("usuario", "deve_trocar_senha"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE usuario
                    ADD COLUMN deve_trocar_senha TINYINT(1) NOT NULL DEFAULT 0;
                ");
            }

            if (!await ColunaExisteAsync("usuario", "origem_conta"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE usuario
                    ADD COLUMN origem_conta VARCHAR(30) NULL;
                ");
            }

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS usuario_recuperacao_senha (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  usuario_id BIGINT UNSIGNED NOT NULL,
                  token_hash VARCHAR(128) NOT NULL,
                  contexto VARCHAR(20) NOT NULL,
                  expira_em DATETIME NOT NULL,
                  usado_em DATETIME NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_usuario_recuperacao_usuario (usuario_id),
                  KEY ix_usuario_recuperacao_token (token_hash),
                  KEY ix_usuario_recuperacao_contexto (contexto),
                  KEY ix_usuario_recuperacao_expira (expira_em),
                  KEY ix_usuario_recuperacao_usado (usado_em),
                  CONSTRAINT fk_usuario_recuperacao_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ministerio_voluntario (
                  ministerio_id BIGINT UNSIGNED NOT NULL,
                  voluntario_id BIGINT UNSIGNED NOT NULL,
                  principal TINYINT(1) NOT NULL DEFAULT 0,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (ministerio_id, voluntario_id),
                  CONSTRAINT fk_ministerio_voluntario_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE CASCADE,
                  CONSTRAINT fk_ministerio_voluntario_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ministerio_funcao_padrao (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  ministerio_id BIGINT UNSIGNED NOT NULL,
                  nome VARCHAR(120) NOT NULL,
                  ordem INT NOT NULL DEFAULT 0,
                  ativo TINYINT(1) NOT NULL DEFAULT 1,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_ministerio_funcao_padrao_ministerio (ministerio_id),
                  KEY ix_ministerio_funcao_padrao_ativo (ativo),
                  UNIQUE KEY uq_ministerio_funcao_padrao_nome (ministerio_id, nome),
                  CONSTRAINT fk_ministerio_funcao_padrao_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await SincronizarVinculosVoluntarioMinisterioAsync();

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS voluntario_acesso_ativacao (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  voluntario_id BIGINT UNSIGNED NOT NULL,
                  codigo_hash VARCHAR(128) NOT NULL,
                  expira_em DATETIME NOT NULL,
                  usado_em DATETIME NULL,
                  tentativas INT NOT NULL DEFAULT 0,
                  canal VARCHAR(30) NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_voluntario_ativacao_voluntario (voluntario_id),
                  KEY ix_voluntario_ativacao_expira (expira_em),
                  KEY ix_voluntario_ativacao_usado (usado_em),
                  CONSTRAINT fk_voluntario_ativacao_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS voluntario_acesso_recuperacao (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  voluntario_id BIGINT UNSIGNED NOT NULL,
                  codigo_hash VARCHAR(128) NOT NULL,
                  expira_em DATETIME NOT NULL,
                  usado_em DATETIME NULL,
                  tentativas INT NOT NULL DEFAULT 0,
                  canal VARCHAR(30) NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_voluntario_recuperacao_voluntario (voluntario_id),
                  KEY ix_voluntario_recuperacao_expira (expira_em),
                  KEY ix_voluntario_recuperacao_usado (usado_em),
                  CONSTRAINT fk_voluntario_recuperacao_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS voluntario_google_calendar_conexao (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  voluntario_id BIGINT UNSIGNED NOT NULL,
                  google_email VARCHAR(180) NOT NULL,
                  google_sub VARCHAR(150) NULL,
                  access_token VARCHAR(2048) NOT NULL,
                  refresh_token VARCHAR(2048) NOT NULL,
                  access_token_expira_em DATETIME NULL,
                  calendario_google_id VARCHAR(255) NULL,
                  calendario_google_nome VARCHAR(255) NULL,
                  ativo TINYINT(1) NOT NULL DEFAULT 1,
                  ultimo_sync_em DATETIME NULL,
                  ultimo_erro_sync VARCHAR(1000) NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  UNIQUE KEY uq_voluntario_google_calendar_conexao_voluntario (voluntario_id),
                  KEY ix_voluntario_google_calendar_conexao_ativo (ativo),
                  CONSTRAINT fk_voluntario_google_calendar_conexao_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS voluntario_google_calendar_evento (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  voluntario_id BIGINT UNSIGNED NOT NULL,
                  escala_id BIGINT UNSIGNED NOT NULL,
                  calendario_google_id VARCHAR(255) NOT NULL,
                  evento_google_id VARCHAR(255) NOT NULL,
                  ultima_sincronizacao_em DATETIME NULL,
                  ultimo_erro_sync VARCHAR(1000) NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  UNIQUE KEY uq_voluntario_google_calendar_evento_escala (escala_id),
                  KEY ix_voluntario_google_calendar_evento_voluntario (voluntario_id),
                  CONSTRAINT fk_voluntario_google_calendar_evento_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE CASCADE,
                  CONSTRAINT fk_voluntario_google_calendar_evento_escala FOREIGN KEY (escala_id) REFERENCES escala(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS musica (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  titulo VARCHAR(180) NOT NULL,
                  artista_banda VARCHAR(180) NOT NULL,
                  tom VARCHAR(20) NULL,
                  link_cifra VARCHAR(500) NULL,
                  link_video VARCHAR(500) NULL,
                  observacoes VARCHAR(500) NULL,
                  ativo TINYINT(1) NOT NULL DEFAULT 1,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS culto_recorrencia (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  nome VARCHAR(150) NOT NULL,
                  tipo_culto VARCHAR(80) NOT NULL,
                  dia_semana INT NOT NULL,
                  horario_inicio TIME NOT NULL,
                  horario_fim_previsto TIME NULL,
                  status_culto_id BIGINT UNSIGNED NOT NULL,
                  observacoes_gerais VARCHAR(800) NULL,
                  template_culto_id BIGINT UNSIGNED NULL,
                  quantidade_semanas_antecedencia INT NOT NULL DEFAULT 12,
                  ativo TINYINT(1) NOT NULL DEFAULT 1,
                  ultima_geracao_em DATETIME NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_culto_recorrencia_ativo (ativo),
                  KEY ix_culto_recorrencia_dia_horario (dia_semana, horario_inicio),
                  KEY ix_culto_recorrencia_template (template_culto_id),
                  CONSTRAINT fk_culto_recorrencia_status FOREIGN KEY (status_culto_id) REFERENCES status_culto(id) ON DELETE RESTRICT,
                  CONSTRAINT fk_culto_recorrencia_template FOREIGN KEY (template_culto_id) REFERENCES template_culto(id) ON DELETE SET NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            if (!await ColunaExisteAsync("culto", "recorrencia_id"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE culto
                    ADD COLUMN recorrencia_id BIGINT UNSIGNED NULL AFTER template_culto_id;
                ");
            }

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS repertorio_culto (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  culto_id BIGINT UNSIGNED NOT NULL,
                  observacoes VARCHAR(500) NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  UNIQUE KEY uq_repertorio_culto (culto_id),
                  CONSTRAINT fk_repertorio_culto FOREIGN KEY (culto_id) REFERENCES culto(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS repertorio_culto_item (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  repertorio_culto_id BIGINT UNSIGNED NOT NULL,
                  musica_id BIGINT UNSIGNED NOT NULL,
                  etapa_culto_id BIGINT UNSIGNED NULL,
                  ordem INT NOT NULL,
                  musica_titulo VARCHAR(180) NULL,
                  musica_artista_banda VARCHAR(180) NULL,
                  musica_tom VARCHAR(20) NULL,
                  musica_link_cifra VARCHAR(500) NULL,
                  musica_link_video VARCHAR(500) NULL,
                  musica_observacoes VARCHAR(500) NULL,
                  responsavel VARCHAR(150) NULL,
                  observacoes VARCHAR(500) NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_rep_item_repertorio (repertorio_culto_id),
                  KEY ix_rep_item_musica (musica_id),
                  CONSTRAINT fk_rep_item_repertorio FOREIGN KEY (repertorio_culto_id) REFERENCES repertorio_culto(id) ON DELETE CASCADE,
                  CONSTRAINT fk_rep_item_musica FOREIGN KEY (musica_id) REFERENCES musica(id) ON DELETE RESTRICT,
                  CONSTRAINT fk_rep_item_etapa FOREIGN KEY (etapa_culto_id) REFERENCES etapa_culto(id) ON DELETE SET NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            if (!await ColunaExisteAsync("repertorio_culto_item", "musica_titulo"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE repertorio_culto_item
                    ADD COLUMN musica_titulo VARCHAR(180) NULL AFTER ordem;
                ");
            }

            if (!await ColunaExisteAsync("repertorio_culto_item", "musica_artista_banda"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE repertorio_culto_item
                    ADD COLUMN musica_artista_banda VARCHAR(180) NULL AFTER musica_titulo;
                ");
            }

            if (!await ColunaExisteAsync("repertorio_culto_item", "musica_tom"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE repertorio_culto_item
                    ADD COLUMN musica_tom VARCHAR(20) NULL AFTER musica_artista_banda;
                ");
            }

            if (!await ColunaExisteAsync("repertorio_culto_item", "musica_link_cifra"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE repertorio_culto_item
                    ADD COLUMN musica_link_cifra VARCHAR(500) NULL AFTER musica_tom;
                ");
            }

            if (!await ColunaExisteAsync("repertorio_culto_item", "musica_link_video"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE repertorio_culto_item
                    ADD COLUMN musica_link_video VARCHAR(500) NULL AFTER musica_link_cifra;
                ");
            }

            if (!await ColunaExisteAsync("repertorio_culto_item", "musica_observacoes"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE repertorio_culto_item
                    ADD COLUMN musica_observacoes VARCHAR(500) NULL AFTER musica_link_video;
                ");
            }

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS template_etapa_culto (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  template_culto_id BIGINT UNSIGNED NOT NULL,
                  sequencia INT NOT NULL,
                  horario_inicial_padrao TIME NULL,
                  duracao_minutos INT NOT NULL,
                  atividade VARCHAR(150) NOT NULL,
                  bloco_cronograma VARCHAR(80) NOT NULL DEFAULT 'PRINCIPAL',
                  descricao VARCHAR(500) NULL,
                  ministerio_responsavel_id BIGINT UNSIGNED NULL,
                  observacoes VARCHAR(500) NULL,
                  status_etapa_id BIGINT UNSIGNED NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_template_etapa_template (template_culto_id),
                  KEY ix_template_etapa_sequencia (sequencia),
                  KEY ix_template_etapa_ministerio (ministerio_responsavel_id),
                  CONSTRAINT fk_template_etapa_template FOREIGN KEY (template_culto_id) REFERENCES template_culto(id) ON DELETE CASCADE,
                  CONSTRAINT fk_template_etapa_ministerio FOREIGN KEY (ministerio_responsavel_id) REFERENCES ministerio(id) ON DELETE SET NULL,
                  CONSTRAINT fk_template_etapa_status FOREIGN KEY (status_etapa_id) REFERENCES status_etapa(id) ON DELETE SET NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            if (!await ColunaExisteAsync("etapa_culto", "bloco_cronograma"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE etapa_culto
                    ADD COLUMN bloco_cronograma VARCHAR(80) NOT NULL DEFAULT 'PRINCIPAL' AFTER atividade;
                ");
            }

            if (!await ColunaExisteAsync("template_etapa_culto", "bloco_cronograma"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE template_etapa_culto
                    ADD COLUMN bloco_cronograma VARCHAR(80) NOT NULL DEFAULT 'PRINCIPAL' AFTER atividade;
                ");
            }

            await _db.Database.ExecuteSqlRawAsync(@"
                UPDATE etapa_culto
                SET bloco_cronograma = 'PRINCIPAL'
                WHERE bloco_cronograma IS NULL OR TRIM(bloco_cronograma) = '';
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                UPDATE template_etapa_culto
                SET bloco_cronograma = 'PRINCIPAL'
                WHERE bloco_cronograma IS NULL OR TRIM(bloco_cronograma) = '';
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS template_etapa_culto_ministerio_acao (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  template_etapa_culto_id BIGINT UNSIGNED NOT NULL,
                  ministerio_id BIGINT UNSIGNED NOT NULL,
                  ordem INT NULL,
                  descricao_acao VARCHAR(500) NOT NULL,
                  observacao VARCHAR(500) NULL,
                  ativo TINYINT(1) NOT NULL DEFAULT 1,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_template_etapa_acao_etapa (template_etapa_culto_id),
                  KEY ix_template_etapa_acao_ministerio (ministerio_id),
                  KEY ix_template_etapa_acao_ordem (ordem),
                  CONSTRAINT fk_template_etapa_acao_etapa FOREIGN KEY (template_etapa_culto_id) REFERENCES template_etapa_culto(id) ON DELETE CASCADE,
                  CONSTRAINT fk_template_etapa_acao_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS template_etapa_culto_ministerio (
                  template_etapa_culto_id BIGINT UNSIGNED NOT NULL,
                  ministerio_id BIGINT UNSIGNED NOT NULL,
                  PRIMARY KEY (template_etapa_culto_id, ministerio_id),
                  KEY ix_template_etapa_ministerio_ministerio (ministerio_id),
                  CONSTRAINT fk_template_etapa_ministerio_link_etapa FOREIGN KEY (template_etapa_culto_id) REFERENCES template_etapa_culto(id) ON DELETE CASCADE,
                  CONSTRAINT fk_template_etapa_ministerio_link_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS etapa_culto_ministerio_acao (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  etapa_culto_id BIGINT UNSIGNED NOT NULL,
                  ministerio_id BIGINT UNSIGNED NOT NULL,
                  ordem INT NULL,
                  descricao_acao VARCHAR(500) NOT NULL,
                  observacao VARCHAR(500) NULL,
                  ativo TINYINT(1) NOT NULL DEFAULT 1,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  KEY ix_etapa_acao_etapa (etapa_culto_id),
                  KEY ix_etapa_acao_ministerio (ministerio_id),
                  KEY ix_etapa_acao_ordem (ordem),
                  CONSTRAINT fk_etapa_acao_etapa FOREIGN KEY (etapa_culto_id) REFERENCES etapa_culto(id) ON DELETE CASCADE,
                  CONSTRAINT fk_etapa_acao_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            if (await TabelaExisteAsync("escala"))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE escala
                    MODIFY COLUMN voluntario_id BIGINT UNSIGNED NULL;
                ");

                if (!await ColunaExisteAsync("escala", "voluntario_avulso_nome"))
                {
                    await _db.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE escala
                        ADD COLUMN voluntario_avulso_nome VARCHAR(160) NULL AFTER voluntario_id;
                    ");
                }

                if (!await ColunaExisteAsync("escala", "voluntario_avulso_telefone"))
                {
                    await _db.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE escala
                        ADD COLUMN voluntario_avulso_telefone VARCHAR(40) NULL AFTER voluntario_avulso_nome;
                    ");
                }
            }

            await SanearEtapasCronogramaDuplicadasAsync();

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS status_disponibilidade_voluntario (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  nome VARCHAR(80) NOT NULL,
                  codigo VARCHAR(50) NOT NULL,
                  cor_hex VARCHAR(7) NULL,
                  ordem INT NOT NULL DEFAULT 0,
                  PRIMARY KEY (id),
                  UNIQUE KEY uq_status_disp_vol_codigo (codigo),
                  UNIQUE KEY uq_status_disp_vol_nome (nome)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS disponibilidade_culto_voluntario (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  culto_id BIGINT UNSIGNED NOT NULL,
                  voluntario_id BIGINT UNSIGNED NOT NULL,
                  status_disponibilidade_id BIGINT UNSIGNED NOT NULL,
                  observacao VARCHAR(500) NULL,
                  respondido_em DATETIME NOT NULL,
                  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  atualizado_em DATETIME NULL,
                  PRIMARY KEY (id),
                  UNIQUE KEY uq_disp_culto_voluntario (culto_id, voluntario_id),
                  KEY ix_disp_culto (culto_id),
                  KEY ix_disp_voluntario (voluntario_id),
                  KEY ix_disp_status (status_disponibilidade_id),
                  KEY ix_disp_respondido_em (respondido_em),
                  CONSTRAINT fk_disp_culto FOREIGN KEY (culto_id) REFERENCES culto(id) ON DELETE CASCADE,
                  CONSTRAINT fk_disp_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE CASCADE,
                  CONSTRAINT fk_disp_status FOREIGN KEY (status_disponibilidade_id) REFERENCES status_disponibilidade_voluntario(id) ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS disponibilidade_culto_voluntario_ministerio (
                  disponibilidade_culto_voluntario_id BIGINT UNSIGNED NOT NULL,
                  ministerio_id BIGINT UNSIGNED NOT NULL,
                  PRIMARY KEY (disponibilidade_culto_voluntario_id, ministerio_id),
                  KEY ix_disp_ministerio_ministerio (ministerio_id),
                  CONSTRAINT fk_disp_ministerio_disp FOREIGN KEY (disponibilidade_culto_voluntario_id) REFERENCES disponibilidade_culto_voluntario(id) ON DELETE CASCADE,
                  CONSTRAINT fk_disp_ministerio_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await GarantirColunasAuditoriaAsync("status_culto");
            await GarantirColunasAuditoriaAsync("status_etapa");
            await GarantirColunasAuditoriaAsync("presenca_escala_status");

            if (!_db.Perfis.Any())
            {
                _db.Perfis.AddRange(
                    new Perfil { Nome = "Administrador", Codigo = "ADMIN", Ativo = true, CriadoEm = DateTime.UtcNow },
                    new Perfil { Nome = "Gestão de Culto", Codigo = "GESTAO_CULTO", Ativo = true, CriadoEm = DateTime.UtcNow },
                    new Perfil { Nome = "Líder de Ministério", Codigo = "LIDER_MINISTERIO", Ativo = true, CriadoEm = DateTime.UtcNow },
                    new Perfil { Nome = "Voluntário", Codigo = "VOLUNTARIO", Ativo = true, CriadoEm = DateTime.UtcNow },
                    new Perfil { Nome = "Recepção / Dados", Codigo = "RECEPCAO_DADOS", Ativo = true, CriadoEm = DateTime.UtcNow }
                );
            }

            var statusCultoAtivo = await _db.StatusCultos.FirstOrDefaultAsync(x => x.Codigo == "ATIVO")
                ?? await _db.StatusCultos.FirstOrDefaultAsync(x => x.Id == 1);
            if (statusCultoAtivo == null)
            {
                statusCultoAtivo = new StatusCulto
                {
                    Nome = "Ativo",
                    Codigo = "ATIVO",
                    CorHex = "#198754",
                    Ordem = 1,
                    CriadoEm = DateTime.UtcNow
                };
                _db.StatusCultos.Add(statusCultoAtivo);
            }
            else
            {
                statusCultoAtivo.Nome = "Ativo";
                statusCultoAtivo.Codigo = "ATIVO";
                statusCultoAtivo.CorHex = "#198754";
                statusCultoAtivo.Ordem = 1;
            }

            var statusCultoInativo = await _db.StatusCultos.FirstOrDefaultAsync(x => x.Codigo == "INATIVO")
                ?? await _db.StatusCultos.FirstOrDefaultAsync(x => x.Id == 2);
            if (statusCultoInativo == null)
            {
                statusCultoInativo = new StatusCulto
                {
                    Nome = "Inativo",
                    Codigo = "INATIVO",
                    CorHex = "#6C757D",
                    Ordem = 2,
                    CriadoEm = DateTime.UtcNow
                };
                _db.StatusCultos.Add(statusCultoInativo);
            }
            else
            {
                statusCultoInativo.Nome = "Inativo";
                statusCultoInativo.Codigo = "INATIVO";
                statusCultoInativo.CorHex = "#6C757D";
                statusCultoInativo.Ordem = 2;
            }

            await _db.SaveChangesAsync();

            await _db.Database.ExecuteSqlRawAsync(@"
                UPDATE culto
                SET status_culto_id = {0}
                WHERE status_culto_id <> {1}
                  AND status_culto_id <> {0};
            ", statusCultoInativo.Id, statusCultoAtivo.Id);

            if (!_db.StatusEtapas.Any())
            {
                _db.StatusEtapas.AddRange(
                    new StatusEtapa { Nome = "Pendente", Codigo = "PENDENTE", CorHex = "#6C757D", Ordem = 1 },
                    new StatusEtapa { Nome = "Em andamento", Codigo = "EM_ANDAMENTO", CorHex = "#FD7E14", Ordem = 2 },
                    new StatusEtapa { Nome = "Concluída", Codigo = "CONCLUIDA", CorHex = "#198754", Ordem = 3 }
                );
            }

            if (!_db.PresencaEscalaStatus.Any())
            {
                _db.PresencaEscalaStatus.AddRange(
                    new PresencaEscalaStatus { Nome = "Pendente", Codigo = "PENDENTE", CorHex = "#6C757D", Ordem = 1 },
                    new PresencaEscalaStatus { Nome = "Confirmado", Codigo = "CONFIRMADO", CorHex = "#198754", Ordem = 2 },
                    new PresencaEscalaStatus { Nome = "Ausente", Codigo = "AUSENTE", CorHex = "#DC3545", Ordem = 3 },
                    new PresencaEscalaStatus { Nome = "Substituído", Codigo = "SUBSTITUIDO", CorHex = "#FD7E14", Ordem = 4 }
                );
            }

            if (!_db.StatusDisponibilidadeVoluntarios.Any())
            {
                _db.StatusDisponibilidadeVoluntarios.AddRange(
                    new StatusDisponibilidadeVoluntario { Nome = "Disponível", Codigo = "DISPONIVEL", CorHex = "#198754", Ordem = 1 },
                    new StatusDisponibilidadeVoluntario { Nome = "Indisponível", Codigo = "INDISPONIVEL", CorHex = "#DC3545", Ordem = 2 },
                    new StatusDisponibilidadeVoluntario { Nome = "Em análise", Codigo = "EM_ANALISE", CorHex = "#FD7E14", Ordem = 3 }
                );
            }

            if (!_db.Ministerios.Any())
            {
                _db.Ministerios.AddRange(
                    new Ministerio { Nome = "Louvor", Codigo = "LOUVOR", Ativo = true, CriadoEm = DateTime.UtcNow },
                    new Ministerio { Nome = "Recepção", Codigo = "RECEPCAO", Ativo = true, CriadoEm = DateTime.UtcNow },
                    new Ministerio { Nome = "Dados", Codigo = "DADOS", Ativo = true, CriadoEm = DateTime.UtcNow },
                    new Ministerio { Nome = "Som", Codigo = "SOM", Ativo = true, CriadoEm = DateTime.UtcNow }
                );
            }

            await _db.SaveChangesAsync();

            if (!_db.Usuarios.Any())
            {
                var admin = new Usuario
                {
                    Nome = "Administrador",
                    Email = "admin@gestaoculto.local",
                    SenhaHash = _passwordHasher.Hash("Admin@123"),
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow
                };

                _db.Usuarios.Add(admin);
                await _db.SaveChangesAsync();

                var perfilAdmin = _db.Perfis.First(x => x.Codigo == "ADMIN");
                _db.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = admin.Id, PerfilId = perfilAdmin.Id });
                await _db.SaveChangesAsync();
            }
        }

        private async Task<bool> ColunaExisteAsync(string tabela, string coluna)
        {
            var connection = _db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = @tabela
                  AND column_name = @coluna";

            var paramTabela = command.CreateParameter();
            paramTabela.ParameterName = "@tabela";
            paramTabela.Value = tabela;
            command.Parameters.Add(paramTabela);

            var paramColuna = command.CreateParameter();
            paramColuna.ParameterName = "@coluna";
            paramColuna.Value = coluna;
            command.Parameters.Add(paramColuna);

            var result = await command.ExecuteScalarAsync();
            var total = Convert.ToInt32(result ?? 0);
            return total > 0;
        }

        private async Task<bool> TabelaExisteAsync(string tabela)
        {
            var connection = _db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = DATABASE()
                  AND table_name = @tabela";

            var paramTabela = command.CreateParameter();
            paramTabela.ParameterName = "@tabela";
            paramTabela.Value = tabela;
            command.Parameters.Add(paramTabela);

            var result = await command.ExecuteScalarAsync();
            var total = Convert.ToInt32(result ?? 0);
            return total > 0;
        }

        private async Task SincronizarVinculosVoluntarioMinisterioAsync()
        {
            var tabelaLegadaExiste = await TabelaExisteAsync("voluntario_ministerio");
            var tabelaAtualExiste = await TabelaExisteAsync("ministerio_voluntario");

            if (!tabelaLegadaExiste || !tabelaAtualExiste)
            {
                return;
            }

            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT IGNORE INTO ministerio_voluntario (ministerio_id, voluntario_id, principal, criado_em)
                SELECT vm.ministerio_id,
                       vm.voluntario_id,
                       vm.principal,
                       COALESCE(vm.criado_em, UTC_TIMESTAMP())
                FROM voluntario_ministerio vm;
            ");
        }

        private async Task GarantirColunasAuditoriaAsync(string tabela)
        {
            if (!await ColunaExisteAsync(tabela, "criado_em"))
            {
                await _db.Database.ExecuteSqlRawAsync($@"
                    ALTER TABLE {tabela}
                    ADD COLUMN criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;
                ");
            }

            if (!await ColunaExisteAsync(tabela, "atualizado_em"))
            {
                await _db.Database.ExecuteSqlRawAsync($@"
                    ALTER TABLE {tabela}
                    ADD COLUMN atualizado_em DATETIME NULL;
                ");
            }
        }

        private async Task SanearEtapasCronogramaDuplicadasAsync()
        {
            if (!await TabelaExisteAsync("etapa_culto"))
            {
                return;
            }

            var subqueryDuplicadas = @"
                SELECT d.id AS duplicate_id,
                       base.keep_id
                FROM etapa_culto d
                INNER JOIN (
                    SELECT culto_id,
                           sequencia,
                           LOWER(TRIM(COALESCE(bloco_cronograma, 'PRINCIPAL'))) AS bloco_normalizado,
                           MIN(id) AS keep_id,
                           COUNT(*) AS total
                    FROM etapa_culto
                    GROUP BY culto_id,
                             sequencia,
                             LOWER(TRIM(COALESCE(bloco_cronograma, 'PRINCIPAL')))
                    HAVING COUNT(*) > 1
                ) base
                    ON base.culto_id = d.culto_id
                   AND base.sequencia = d.sequencia
                   AND base.bloco_normalizado = LOWER(TRIM(COALESCE(d.bloco_cronograma, 'PRINCIPAL')))
                WHERE d.id <> base.keep_id";

            if (await TabelaExisteAsync("escala"))
            {
                await _db.Database.ExecuteSqlRawAsync($@"
                    UPDATE escala e
                    INNER JOIN ({subqueryDuplicadas}) mapa
                        ON mapa.duplicate_id = e.etapa_culto_id
                    SET e.etapa_culto_id = mapa.keep_id;
                ");
            }

            if (await TabelaExisteAsync("repertorio_culto_item"))
            {
                await _db.Database.ExecuteSqlRawAsync($@"
                    UPDATE repertorio_culto_item r
                    INNER JOIN ({subqueryDuplicadas}) mapa
                        ON mapa.duplicate_id = r.etapa_culto_id
                    SET r.etapa_culto_id = mapa.keep_id;
                ");
            }

            if (await TabelaExisteAsync("etapa_culto_ministerio_acao"))
            {
                await _db.Database.ExecuteSqlRawAsync($@"
                    UPDATE etapa_culto_ministerio_acao a
                    INNER JOIN ({subqueryDuplicadas}) mapa
                        ON mapa.duplicate_id = a.etapa_culto_id
                    SET a.etapa_culto_id = mapa.keep_id;
                ");
            }

            await _db.Database.ExecuteSqlRawAsync($@"
                DELETE d
                FROM etapa_culto d
                INNER JOIN ({subqueryDuplicadas}) mapa
                    ON mapa.duplicate_id = d.id;
            ");
        }
    }
}
