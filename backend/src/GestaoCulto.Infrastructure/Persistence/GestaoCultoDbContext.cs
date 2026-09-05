using System;
using GestaoCulto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.Infrastructure.Persistence
{
    public class GestaoCultoDbContext : DbContext
    {
        public GestaoCultoDbContext(DbContextOptions<GestaoCultoDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<UsuarioRecuperacaoSenha> UsuariosRecuperacaoSenha => Set<UsuarioRecuperacaoSenha>();
        public DbSet<Perfil> Perfis => Set<Perfil>();
        public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();
        public DbSet<UsuarioGoogle> UsuariosGoogle => Set<UsuarioGoogle>();
        public DbSet<Ministerio> Ministerios => Set<Ministerio>();
        public DbSet<MinisterioFuncaoPadrao> MinisteriosFuncoesPadrao => Set<MinisterioFuncaoPadrao>();
        public DbSet<MinisterioLider> MinisteriosLideres => Set<MinisterioLider>();
        public DbSet<MinisterioVoluntario> MinisteriosVoluntarios => Set<MinisterioVoluntario>();
        public DbSet<Voluntario> Voluntarios => Set<Voluntario>();
        public DbSet<VoluntarioMinisterio> VoluntariosMinisterios => Set<VoluntarioMinisterio>();
        public DbSet<VoluntarioAcessoAtivacao> VoluntariosAcessoAtivacao => Set<VoluntarioAcessoAtivacao>();
        public DbSet<VoluntarioAcessoRecuperacao> VoluntariosAcessoRecuperacao => Set<VoluntarioAcessoRecuperacao>();
        public DbSet<VoluntarioGoogleCalendarConexao> VoluntariosGoogleCalendarConexoes => Set<VoluntarioGoogleCalendarConexao>();
        public DbSet<VoluntarioGoogleCalendarEvento> VoluntariosGoogleCalendarEventos => Set<VoluntarioGoogleCalendarEvento>();
        public DbSet<VoluntarioTelegramConexao> VoluntariosTelegramConexoes => Set<VoluntarioTelegramConexao>();
        public DbSet<VoluntarioTelegramVinculoToken> VoluntariosTelegramVinculoTokens => Set<VoluntarioTelegramVinculoToken>();
        public DbSet<TelegramUpdateProcessado> TelegramUpdatesProcessados => Set<TelegramUpdateProcessado>();
        public DbSet<TelegramDisponibilidadeRascunho> TelegramDisponibilidadeRascunhos => Set<TelegramDisponibilidadeRascunho>();
        public DbSet<TelegramDisponibilidadeRascunhoMinisterio> TelegramDisponibilidadeRascunhosMinisterios => Set<TelegramDisponibilidadeRascunhoMinisterio>();
        public DbSet<TelegramRepertorioRascunho> TelegramRepertoriosRascunhos => Set<TelegramRepertorioRascunho>();
        public DbSet<RelatorioCultoCompartilhamento> RelatoriosCultoCompartilhamentos => Set<RelatorioCultoCompartilhamento>();
        public DbSet<StatusCulto> StatusCultos => Set<StatusCulto>();
        public DbSet<StatusEtapa> StatusEtapas => Set<StatusEtapa>();
        public DbSet<PresencaEscalaStatus> PresencaEscalaStatus => Set<PresencaEscalaStatus>();
        public DbSet<StatusDisponibilidadeVoluntario> StatusDisponibilidadeVoluntarios => Set<StatusDisponibilidadeVoluntario>();
        public DbSet<TemplateCulto> TemplatesCulto => Set<TemplateCulto>();
        public DbSet<TemplateEtapaCulto> TemplatesEtapasCulto => Set<TemplateEtapaCulto>();
        public DbSet<TemplateEtapaCultoMinisterio> TemplatesEtapasCultoMinisterios => Set<TemplateEtapaCultoMinisterio>();
        public DbSet<TemplateEtapaCultoMinisterioAcao> TemplatesEtapasCultoMinisteriosAcoes => Set<TemplateEtapaCultoMinisterioAcao>();
        public DbSet<Musica> Musicas => Set<Musica>();
        public DbSet<Culto> Cultos => Set<Culto>();
        public DbSet<CultoRecorrencia> CultosRecorrencias => Set<CultoRecorrencia>();
        public DbSet<RepertorioCulto> RepertoriosCulto => Set<RepertorioCulto>();
        public DbSet<RepertorioCultoItem> RepertoriosCultoItens => Set<RepertorioCultoItem>();
        public DbSet<EtapaCulto> EtapasCulto => Set<EtapaCulto>();
        public DbSet<EtapaCultoMinisterioAcao> EtapasCultoMinisteriosAcoes => Set<EtapaCultoMinisterioAcao>();
        public DbSet<Escala> Escalas => Set<Escala>();
        public DbSet<DisponibilidadeCultoVoluntario> DisponibilidadesCultoVoluntarios => Set<DisponibilidadeCultoVoluntario>();
        public DbSet<DisponibilidadeCultoVoluntarioMinisterio> DisponibilidadesCultoVoluntariosMinisterios => Set<DisponibilidadeCultoVoluntarioMinisterio>();
        public DbSet<Convidado> Convidados => Set<Convidado>();
        public DbSet<NovoConvertido> NovosConvertidos => Set<NovoConvertido>();
        public DbSet<Auditoria> Auditorias => Set<Auditoria>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuario");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
                entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(180).IsRequired();
                entity.Property(x => x.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255).IsRequired();
                entity.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(30);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.DeveTrocarSenha).HasColumnName("deve_trocar_senha");
                entity.Property(x => x.OrigemConta).HasColumnName("origem_conta").HasMaxLength(30);
                entity.Property(x => x.UltimoLoginEm).HasColumnName("ultimo_login_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<Perfil>(entity =>
            {
                entity.ToTable("perfil");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(60).IsRequired();
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<UsuarioPerfil>(entity =>
            {
                entity.ToTable("usuario_perfil");
                entity.HasKey(x => new { x.UsuarioId, x.PerfilId });
                entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
                entity.Property(x => x.PerfilId).HasColumnName("perfil_id");
                entity.HasOne(x => x.Usuario).WithMany(x => x.Perfis).HasForeignKey(x => x.UsuarioId);
                entity.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId);
            });

            modelBuilder.Entity<UsuarioGoogle>(entity =>
            {
                entity.ToTable("usuario_google");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
                entity.Property(x => x.GoogleSub).HasColumnName("google_sub").HasMaxLength(150).IsRequired();
                entity.Property(x => x.EmailGoogle).HasColumnName("email_google").HasMaxLength(180).IsRequired();
                entity.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
                entity.Property(x => x.VinculadoEm).HasColumnName("vinculado_em");
                entity.Property(x => x.UltimoLoginGoogleEm).HasColumnName("ultimo_login_google_em");
                entity.HasOne(x => x.Usuario).WithOne(x => x.UsuarioGoogle).HasForeignKey<UsuarioGoogle>(x => x.UsuarioId);
            });

            modelBuilder.Entity<UsuarioRecuperacaoSenha>(entity =>
            {
                entity.ToTable("usuario_recuperacao_senha");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
                entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
                entity.Property(x => x.Contexto).HasColumnName("contexto").HasMaxLength(20).IsRequired();
                entity.Property(x => x.ExpiraEm).HasColumnName("expira_em");
                entity.Property(x => x.UsadoEm).HasColumnName("usado_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Usuario).WithMany(x => x.RecuperacoesSenha).HasForeignKey(x => x.UsuarioId);
            });

            modelBuilder.Entity<Ministerio>(entity =>
            {
                entity.ToTable("ministerio");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
                entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(60).IsRequired();
                entity.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(255);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<MinisterioFuncaoPadrao>(entity =>
            {
                entity.ToTable("ministerio_funcao_padrao");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
                entity.Property(x => x.BlocoCronograma).HasColumnName("bloco_cronograma").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Ordem).HasColumnName("ordem");
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.PodeGerenciarRepertorio).HasColumnName("pode_gerenciar_repertorio");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Ministerio).WithMany().HasForeignKey(x => x.MinisterioId);
            });

            modelBuilder.Entity<MinisterioLider>(entity =>
            {
                entity.ToTable("ministerio_lider");
                entity.HasKey(x => new { x.MinisterioId, x.UsuarioId });
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
                entity.Property(x => x.Principal).HasColumnName("principal");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
            });

            modelBuilder.Entity<MinisterioVoluntario>(entity =>
            {
                entity.ToTable("ministerio_voluntario");
                entity.HasKey(x => new { x.MinisterioId, x.VoluntarioId });
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.Principal).HasColumnName("principal");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
            });

            modelBuilder.Entity<Voluntario>(entity =>
            {
                entity.ToTable("voluntario");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
                entity.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(30);
                entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(180);
                entity.Property(x => x.MinisterioPrincipalId).HasColumnName("ministerio_principal_id");
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.RestricoesIndisponibilidade).HasColumnName("restricoes_indisponibilidade").HasMaxLength(500);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<VoluntarioMinisterio>(entity =>
            {
                entity.ToTable("voluntario_ministerio");
                entity.HasKey(x => new { x.VoluntarioId, x.MinisterioId });
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.Property(x => x.Principal).HasColumnName("principal");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
                entity.HasOne(x => x.Ministerio).WithMany().HasForeignKey(x => x.MinisterioId);
            });

            modelBuilder.Entity<VoluntarioAcessoAtivacao>(entity =>
            {
                entity.ToTable("voluntario_acesso_ativacao");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.CodigoHash).HasColumnName("codigo_hash").HasMaxLength(128).IsRequired();
                entity.Property(x => x.ExpiraEm).HasColumnName("expira_em");
                entity.Property(x => x.UsadoEm).HasColumnName("usado_em");
                entity.Property(x => x.Tentativas).HasColumnName("tentativas");
                entity.Property(x => x.Canal).HasColumnName("canal").HasMaxLength(30);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
            });

            modelBuilder.Entity<VoluntarioAcessoRecuperacao>(entity =>
            {
                entity.ToTable("voluntario_acesso_recuperacao");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.CodigoHash).HasColumnName("codigo_hash").HasMaxLength(128).IsRequired();
                entity.Property(x => x.ExpiraEm).HasColumnName("expira_em");
                entity.Property(x => x.UsadoEm).HasColumnName("usado_em");
                entity.Property(x => x.Tentativas).HasColumnName("tentativas");
                entity.Property(x => x.Canal).HasColumnName("canal").HasMaxLength(30);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
            });

            modelBuilder.Entity<VoluntarioGoogleCalendarConexao>(entity =>
            {
                entity.ToTable("voluntario_google_calendar_conexao");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.GoogleEmail).HasColumnName("google_email").HasMaxLength(180).IsRequired();
                entity.Property(x => x.GoogleSub).HasColumnName("google_sub").HasMaxLength(150);
                entity.Property(x => x.AccessToken).HasColumnName("access_token").HasMaxLength(2048).IsRequired();
                entity.Property(x => x.RefreshToken).HasColumnName("refresh_token").HasMaxLength(2048).IsRequired();
                entity.Property(x => x.AccessTokenExpiraEm).HasColumnName("access_token_expira_em");
                entity.Property(x => x.CalendarioGoogleId).HasColumnName("calendario_google_id").HasMaxLength(255);
                entity.Property(x => x.CalendarioGoogleNome).HasColumnName("calendario_google_nome").HasMaxLength(255);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.UltimoSyncEm).HasColumnName("ultimo_sync_em");
                entity.Property(x => x.UltimoErroSync).HasColumnName("ultimo_erro_sync").HasMaxLength(1000);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
            });

            modelBuilder.Entity<VoluntarioGoogleCalendarEvento>(entity =>
            {
                entity.ToTable("voluntario_google_calendar_evento");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.EscalaId).HasColumnName("escala_id");
                entity.Property(x => x.CalendarioGoogleId).HasColumnName("calendario_google_id").HasMaxLength(255).IsRequired();
                entity.Property(x => x.EventoGoogleId).HasColumnName("evento_google_id").HasMaxLength(255).IsRequired();
                entity.Property(x => x.UltimaSincronizacaoEm).HasColumnName("ultima_sincronizacao_em");
                entity.Property(x => x.UltimoErroSync).HasColumnName("ultimo_erro_sync").HasMaxLength(1000);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
                entity.HasOne(x => x.Escala).WithMany().HasForeignKey(x => x.EscalaId);
            });

            modelBuilder.Entity<VoluntarioTelegramConexao>(entity =>
            {
                entity.ToTable("voluntario_telegram_conexao");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.TelegramUserId).HasColumnName("telegram_user_id");
                entity.Property(x => x.TelegramChatId).HasColumnName("telegram_chat_id");
                entity.Property(x => x.TelegramUsername).HasColumnName("telegram_username").HasMaxLength(100);
                entity.Property(x => x.TelegramPrimeiroNome).HasColumnName("telegram_primeiro_nome").HasMaxLength(150);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.ConsentimentoEm).HasColumnName("consentimento_em");
                entity.Property(x => x.ConsentimentoVersao).HasColumnName("consentimento_versao").HasMaxLength(30).IsRequired();
                entity.Property(x => x.VinculadoEm).HasColumnName("vinculado_em");
                entity.Property(x => x.UltimaInteracaoEm).HasColumnName("ultima_interacao_em");
                entity.Property(x => x.DesvinculadoEm).HasColumnName("desvinculado_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasIndex(x => x.VoluntarioId).IsUnique();
                entity.HasIndex(x => x.TelegramUserId).IsUnique();
                entity.HasIndex(x => x.TelegramChatId).IsUnique();
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
            });

            modelBuilder.Entity<VoluntarioTelegramVinculoToken>(entity =>
            {
                entity.ToTable("voluntario_telegram_vinculo_token");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
                entity.Property(x => x.ExpiraEm).HasColumnName("expira_em");
                entity.Property(x => x.UsadoEm).HasColumnName("usado_em");
                entity.Property(x => x.RevogadoEm).HasColumnName("revogado_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
            });

            modelBuilder.Entity<TelegramUpdateProcessado>(entity =>
            {
                entity.ToTable("telegram_update_processado");
                entity.HasKey(x => x.TelegramUpdateId);
                entity.Property(x => x.TelegramUpdateId).HasColumnName("telegram_update_id").ValueGeneratedNever();
                entity.Property(x => x.RecebidoEm).HasColumnName("recebido_em");
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(x => x.ClaimId).HasColumnName("claim_id").HasMaxLength(36);
                entity.Property(x => x.IniciadoEm).HasColumnName("iniciado_em");
                entity.Property(x => x.ConcluidoEm).HasColumnName("concluido_em");
            });

            modelBuilder.Entity<TelegramDisponibilidadeRascunho>(entity =>
            {
                entity.ToTable("telegram_disponibilidade_rascunho");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.ExpiraEm).HasColumnName("expira_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasIndex(x => new { x.VoluntarioId, x.CultoId }).IsUnique();
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
                entity.HasOne(x => x.Culto).WithMany().HasForeignKey(x => x.CultoId);
            });

            modelBuilder.Entity<TelegramRepertorioRascunho>(entity =>
            {
                entity.ToTable("telegram_repertorio_rascunho");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.MusicaIds).HasColumnName("musica_ids").HasMaxLength(4000).IsRequired();
                entity.Property(x => x.AcaoPendente).HasColumnName("acao_pendente").HasMaxLength(30);
                entity.Property(x => x.ExpiraEm).HasColumnName("expira_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Culto).WithMany().HasForeignKey(x => x.CultoId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(x => new { x.VoluntarioId, x.CultoId }).IsUnique();
            });

            modelBuilder.Entity<TelegramDisponibilidadeRascunhoMinisterio>(entity =>
            {
                entity.ToTable("telegram_disponibilidade_rascunho_ministerio");
                entity.HasKey(x => new { x.TelegramDisponibilidadeRascunhoId, x.MinisterioId });
                entity.Property(x => x.TelegramDisponibilidadeRascunhoId).HasColumnName("telegram_disponibilidade_rascunho_id");
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.HasOne(x => x.TelegramDisponibilidadeRascunho)
                    .WithMany()
                    .HasForeignKey(x => x.TelegramDisponibilidadeRascunhoId);
                entity.HasOne(x => x.Ministerio).WithMany().HasForeignKey(x => x.MinisterioId);
            });

            modelBuilder.Entity<RelatorioCultoCompartilhamento>(entity =>
            {
                entity.ToTable("relatorio_culto_compartilhamento");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id").IsRequired(false);
                entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
                entity.Property(x => x.ExpiraEm).HasColumnName("expira_em");
                entity.Property(x => x.UltimoAcessoEm).HasColumnName("ultimo_acesso_em");
                entity.Property(x => x.RevogadoEm).HasColumnName("revogado_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.ExpiraEm);
                entity.HasOne(x => x.Culto).WithMany().HasForeignKey(x => x.CultoId);
                entity.HasOne(x => x.Voluntario).WithMany().HasForeignKey(x => x.VoluntarioId);
            });

            modelBuilder.Entity<StatusCulto>(entity =>
            {
                entity.ToTable("status_culto");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(x => x.CorHex).HasColumnName("cor_hex").HasMaxLength(7);
                entity.Property(x => x.Ordem).HasColumnName("ordem");
                entity.Ignore(x => x.CriadoEm);
                entity.Ignore(x => x.AtualizadoEm);
            });

            modelBuilder.Entity<StatusEtapa>(entity =>
            {
                entity.ToTable("status_etapa");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(x => x.CorHex).HasColumnName("cor_hex").HasMaxLength(7);
                entity.Property(x => x.Ordem).HasColumnName("ordem");
                entity.Ignore(x => x.CriadoEm);
                entity.Ignore(x => x.AtualizadoEm);
            });

            modelBuilder.Entity<PresencaEscalaStatus>(entity =>
            {
                entity.ToTable("presenca_escala_status");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(x => x.CorHex).HasColumnName("cor_hex").HasMaxLength(7);
                entity.Property(x => x.Ordem).HasColumnName("ordem");
                entity.Ignore(x => x.CriadoEm);
                entity.Ignore(x => x.AtualizadoEm);
            });

            modelBuilder.Entity<StatusDisponibilidadeVoluntario>(entity =>
            {
                entity.ToTable("status_disponibilidade_voluntario");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(x => x.CorHex).HasColumnName("cor_hex").HasMaxLength(7);
                entity.Property(x => x.Ordem).HasColumnName("ordem");
            });

            modelBuilder.Entity<TemplateCulto>(entity =>
            {
                entity.ToTable("template_culto");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
                entity.Property(x => x.TipoCulto).HasColumnName("tipo_culto").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<TemplateEtapaCulto>(entity =>
            {
                entity.ToTable("template_etapa_culto");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.TemplateCultoId).HasColumnName("template_culto_id");
                entity.Property(x => x.Sequencia).HasColumnName("sequencia");
                entity.Property(x => x.HorarioInicialPadrao).HasColumnName("horario_inicial_padrao");
                entity.Property(x => x.DuracaoMinutos).HasColumnName("duracao_minutos");
                entity.Property(x => x.Atividade).HasColumnName("atividade").HasMaxLength(150).IsRequired();
                entity.Property(x => x.BlocoCronograma).HasColumnName("bloco_cronograma").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500);
                entity.Property(x => x.MinisterioResponsavelId).HasColumnName("ministerio_responsavel_id");
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.StatusEtapaId).HasColumnName("status_etapa_id");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.TemplateCulto).WithMany().HasForeignKey(x => x.TemplateCultoId);
            });

            modelBuilder.Entity<TemplateEtapaCultoMinisterioAcao>(entity =>
            {
                entity.ToTable("template_etapa_culto_ministerio_acao");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.TemplateEtapaCultoId).HasColumnName("template_etapa_culto_id");
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.Property(x => x.Ordem).HasColumnName("ordem");
                entity.Property(x => x.DescricaoAcao).HasColumnName("descricao_acao").HasMaxLength(500).IsRequired();
                entity.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(500);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.TemplateEtapaCulto).WithMany(x => x.AcoesMinisterio).HasForeignKey(x => x.TemplateEtapaCultoId);
                entity.HasOne(x => x.Ministerio).WithMany().HasForeignKey(x => x.MinisterioId);
            });

            modelBuilder.Entity<TemplateEtapaCultoMinisterio>(entity =>
            {
                entity.ToTable("template_etapa_culto_ministerio");
                entity.HasKey(x => new { x.TemplateEtapaCultoId, x.MinisterioId });
                entity.Property(x => x.TemplateEtapaCultoId).HasColumnName("template_etapa_culto_id");
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.HasOne(x => x.TemplateEtapaCulto).WithMany().HasForeignKey(x => x.TemplateEtapaCultoId);
                entity.HasOne(x => x.Ministerio).WithMany().HasForeignKey(x => x.MinisterioId);
            });

            modelBuilder.Entity<Musica>(entity =>
            {
                entity.ToTable("musica");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(180).IsRequired();
                entity.Property(x => x.ArtistaBanda).HasColumnName("artista_banda").HasMaxLength(180).IsRequired();
                entity.Property(x => x.Tom).HasColumnName("tom").HasMaxLength(20);
                entity.Property(x => x.LinkCifra).HasColumnName("link_cifra").HasMaxLength(500);
                entity.Property(x => x.LinkVideo).HasColumnName("link_video").HasMaxLength(500);
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<Culto>(entity =>
            {
                entity.ToTable("culto");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.TemplateCultoId).HasColumnName("template_culto_id");
                entity.Property(x => x.RecorrenciaId).HasColumnName("recorrencia_id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
                entity.Property(x => x.TipoCulto).HasColumnName("tipo_culto").HasMaxLength(80).IsRequired();
                entity.Property(x => x.DataCulto).HasColumnName("data_culto");
                entity.Property(x => x.HorarioInicio).HasColumnName("horario_inicio");
                entity.Property(x => x.HorarioFimPrevisto).HasColumnName("horario_fim_previsto");
                entity.Property(x => x.StatusCultoId).HasColumnName("status_culto_id");
                entity.Property(x => x.ObservacoesGerais).HasColumnName("observacoes_gerais").HasMaxLength(800);
                entity.Property(x => x.TotalVisitantes).HasColumnName("total_visitantes");
                entity.Property(x => x.TotalNovosConvertidos).HasColumnName("total_novos_convertidos");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.StatusCulto).WithMany().HasForeignKey(x => x.StatusCultoId);
                entity.HasOne(x => x.Recorrencia).WithMany().HasForeignKey(x => x.RecorrenciaId);
            });

            modelBuilder.Entity<CultoRecorrencia>(entity =>
            {
                entity.ToTable("culto_recorrencia");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
                entity.Property(x => x.TipoCulto).HasColumnName("tipo_culto").HasMaxLength(80).IsRequired();
                entity.Property(x => x.DiaSemana).HasColumnName("dia_semana");
                entity.Property(x => x.HorarioInicio).HasColumnName("horario_inicio");
                entity.Property(x => x.HorarioFimPrevisto).HasColumnName("horario_fim_previsto");
                entity.Property(x => x.StatusCultoId).HasColumnName("status_culto_id");
                entity.Property(x => x.ObservacoesGerais).HasColumnName("observacoes_gerais").HasMaxLength(800);
                entity.Property(x => x.TemplateCultoId).HasColumnName("template_culto_id");
                entity.Property(x => x.QuantidadeSemanasAntecedencia).HasColumnName("quantidade_semanas_antecedencia");
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.UltimaGeracaoEm).HasColumnName("ultima_geracao_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.StatusCulto).WithMany().HasForeignKey(x => x.StatusCultoId);
                entity.HasOne(x => x.TemplateCulto).WithMany().HasForeignKey(x => x.TemplateCultoId);
            });

            modelBuilder.Entity<RepertorioCulto>(entity =>
            {
                entity.ToTable("repertorio_culto");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<RepertorioCultoItem>(entity =>
            {
                entity.ToTable("repertorio_culto_item");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.RepertorioCultoId).HasColumnName("repertorio_culto_id");
                entity.Property(x => x.MusicaId).HasColumnName("musica_id");
                entity.Property(x => x.EtapaCultoId).HasColumnName("etapa_culto_id");
                entity.Property(x => x.Ordem).HasColumnName("ordem");
                entity.Property(x => x.MusicaTitulo).HasColumnName("musica_titulo").HasMaxLength(180);
                entity.Property(x => x.MusicaArtistaBanda).HasColumnName("musica_artista_banda").HasMaxLength(180);
                entity.Property(x => x.MusicaTom).HasColumnName("musica_tom").HasMaxLength(20);
                entity.Property(x => x.MusicaLinkCifra).HasColumnName("musica_link_cifra").HasMaxLength(500);
                entity.Property(x => x.MusicaLinkVideo).HasColumnName("musica_link_video").HasMaxLength(500);
                entity.Property(x => x.MusicaObservacoes).HasColumnName("musica_observacoes").HasMaxLength(500);
                entity.Property(x => x.Responsavel).HasColumnName("responsavel").HasMaxLength(150);
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<EtapaCulto>(entity =>
            {
                entity.ToTable("etapa_culto");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.Sequencia).HasColumnName("sequencia");
                entity.Property(x => x.HorarioInicio).HasColumnName("horario_inicio");
                entity.Property(x => x.DuracaoMinutos).HasColumnName("duracao_minutos");
                entity.Property(x => x.HorarioFimCalculado).HasColumnName("horario_fim_calculado");
                entity.Property(x => x.Atividade).HasColumnName("atividade").HasMaxLength(150).IsRequired();
                entity.Property(x => x.BlocoCronograma).HasColumnName("bloco_cronograma").HasMaxLength(80).IsRequired();
                entity.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500);
                entity.Property(x => x.ResponsavelPrincipalUsuarioId).HasColumnName("responsavel_principal_usuario_id");
                entity.Property(x => x.MinisterioResponsavelId).HasColumnName("ministerio_responsavel_id");
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.AtrasoMinutos).HasColumnName("atraso_minutos");
                entity.Property(x => x.StatusEtapaId).HasColumnName("status_etapa_id");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.Culto).WithMany().HasForeignKey(x => x.CultoId);
            });

            modelBuilder.Entity<EtapaCultoMinisterioAcao>(entity =>
            {
                entity.ToTable("etapa_culto_ministerio_acao");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.EtapaCultoId).HasColumnName("etapa_culto_id");
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.Property(x => x.Ordem).HasColumnName("ordem");
                entity.Property(x => x.DescricaoAcao).HasColumnName("descricao_acao").HasMaxLength(500).IsRequired();
                entity.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(500);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
                entity.HasOne(x => x.EtapaCulto).WithMany(x => x.AcoesMinisterio).HasForeignKey(x => x.EtapaCultoId);
                entity.HasOne(x => x.Ministerio).WithMany().HasForeignKey(x => x.MinisterioId);
            });

            modelBuilder.Entity<Escala>(entity =>
            {
                entity.ToTable("escala");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.EtapaCultoId).HasColumnName("etapa_culto_id");
                entity.Property(x => x.BlocoCronograma).HasColumnName("bloco_cronograma").HasMaxLength(80);
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.VoluntarioAvulsoNome).HasColumnName("voluntario_avulso_nome").HasMaxLength(160);
                entity.Property(x => x.VoluntarioAvulsoTelefone).HasColumnName("voluntario_avulso_telefone").HasMaxLength(40);
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.Property(x => x.Funcao).HasColumnName("funcao").HasMaxLength(120).IsRequired();
                entity.Property(x => x.PodeGerenciarRepertorio).HasColumnName("pode_gerenciar_repertorio");
                entity.Property(x => x.HorarioPrevisto).HasColumnName("horario_previsto");
                entity.Property(x => x.PresencaStatusId).HasColumnName("presenca_status_id");
                entity.Property(x => x.ConfirmadoEm).HasColumnName("confirmado_em");
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<DisponibilidadeCultoVoluntario>(entity =>
            {
                entity.ToTable("disponibilidade_culto_voluntario");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.VoluntarioId).HasColumnName("voluntario_id");
                entity.Property(x => x.StatusDisponibilidadeId).HasColumnName("status_disponibilidade_id");
                entity.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(500);
                entity.Property(x => x.RespondidoEm).HasColumnName("respondido_em");
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<DisponibilidadeCultoVoluntarioMinisterio>(entity =>
            {
                entity.ToTable("disponibilidade_culto_voluntario_ministerio");
                entity.HasKey(x => new { x.DisponibilidadeCultoVoluntarioId, x.MinisterioId });
                entity.Property(x => x.DisponibilidadeCultoVoluntarioId).HasColumnName("disponibilidade_culto_voluntario_id");
                entity.Property(x => x.MinisterioId).HasColumnName("ministerio_id");
                entity.HasOne(x => x.DisponibilidadeCultoVoluntario).WithMany().HasForeignKey(x => x.DisponibilidadeCultoVoluntarioId);
                entity.HasOne(x => x.Ministerio).WithMany().HasForeignKey(x => x.MinisterioId);
            });

            modelBuilder.Entity<Convidado>(entity =>
            {
                entity.ToTable("convidado");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.CultoId).HasColumnName("culto_id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
                entity.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(30);
                entity.Property(x => x.QuemConvidou).HasColumnName("quem_convidou").HasMaxLength(150);
                entity.Property(x => x.PrimeiraVezIgreja).HasColumnName("primeira_vez_igreja");
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
                entity.Property(x => x.StatusAcompanhamento).HasColumnName("status_acompanhamento").HasMaxLength(30);
                entity.Property(x => x.CriadoEm).HasColumnName("criado_em");
                entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            });

            modelBuilder.Entity<NovoConvertido>(entity =>
            {
                entity.ToTable("novo_convertido");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ConvidadoId).HasColumnName("convidado_id");
                entity.Property(x => x.DataDecisao).HasColumnName("data_decisao");
                entity.Property(x => x.StatusAcompanhamento).HasColumnName("status_acompanhamento").HasMaxLength(30);
                entity.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
            });
        }
    }
}
