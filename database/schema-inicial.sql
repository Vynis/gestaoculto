-- Gestão de Culto - Script inicial
-- Compatível com MySQL/MariaDB 10.2.x

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE DATABASE IF NOT EXISTS gestao_culto
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE gestao_culto;

-- =========================
-- IDENTIDADE E ACESSO
-- =========================

CREATE TABLE IF NOT EXISTS perfil (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome VARCHAR(80) NOT NULL,
  codigo VARCHAR(60) NOT NULL,
  ativo TINYINT(1) NOT NULL DEFAULT 1,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_perfil_codigo (codigo),
  UNIQUE KEY uq_perfil_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS usuario (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome VARCHAR(150) NOT NULL,
  email VARCHAR(180) NOT NULL,
  senha_hash VARCHAR(255) NOT NULL,
  telefone VARCHAR(30) NULL,
  ativo TINYINT(1) NOT NULL DEFAULT 1,
  deve_trocar_senha TINYINT(1) NOT NULL DEFAULT 0,
  origem_conta VARCHAR(30) NULL,
  ultimo_login_em DATETIME NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_usuario_email (email),
  KEY ix_usuario_ativo (ativo),
  CONSTRAINT fk_usuario_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_usuario_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS usuario_perfil (
  usuario_id BIGINT UNSIGNED NOT NULL,
  perfil_id BIGINT UNSIGNED NOT NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (usuario_id, perfil_id),
  KEY ix_usuario_perfil_perfil (perfil_id),
  CONSTRAINT fk_usuario_perfil_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE CASCADE,
  CONSTRAINT fk_usuario_perfil_perfil FOREIGN KEY (perfil_id) REFERENCES perfil(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS usuario_google (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  usuario_id BIGINT UNSIGNED NOT NULL,
  google_sub VARCHAR(150) NOT NULL,
  email_google VARCHAR(180) NOT NULL,
  avatar_url VARCHAR(500) NULL,
  vinculado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ultimo_login_google_em DATETIME NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_usuario_google_usuario (usuario_id),
  UNIQUE KEY uq_usuario_google_sub (google_sub),
  KEY ix_usuario_google_email (email_google),
  CONSTRAINT fk_usuario_google_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================
-- MINISTERIOS E VOLUNTARIOS
-- =========================

CREATE TABLE IF NOT EXISTS ministerio (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome VARCHAR(120) NOT NULL,
  codigo VARCHAR(60) NOT NULL,
  descricao VARCHAR(255) NULL,
  ativo TINYINT(1) NOT NULL DEFAULT 1,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_ministerio_codigo (codigo),
  UNIQUE KEY uq_ministerio_nome (nome),
  KEY ix_ministerio_ativo (ativo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

CREATE TABLE IF NOT EXISTS usuario_ministerio_lider (
  usuario_id BIGINT UNSIGNED NOT NULL,
  ministerio_id BIGINT UNSIGNED NOT NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (usuario_id, ministerio_id),
  KEY ix_usuario_ministerio_lider_ministerio (ministerio_id),
  CONSTRAINT fk_usuario_ministerio_lider_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE CASCADE,
  CONSTRAINT fk_usuario_ministerio_lider_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS voluntario (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  usuario_id BIGINT UNSIGNED NULL,
  nome VARCHAR(150) NOT NULL,
  telefone VARCHAR(30) NULL,
  email VARCHAR(180) NULL,
  ministerio_principal_id BIGINT UNSIGNED NULL,
  observacoes VARCHAR(500) NULL,
  restricoes_indisponibilidade VARCHAR(500) NULL,
  foto_url VARCHAR(500) NULL,
  ativo TINYINT(1) NOT NULL DEFAULT 1,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_voluntario_nome (nome),
  KEY ix_voluntario_ativo (ativo),
  KEY ix_voluntario_ministerio_principal (ministerio_principal_id),
  KEY ix_voluntario_usuario (usuario_id),
  CONSTRAINT fk_voluntario_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE SET NULL,
  CONSTRAINT fk_voluntario_ministerio_principal FOREIGN KEY (ministerio_principal_id) REFERENCES ministerio(id) ON DELETE SET NULL,
  CONSTRAINT fk_voluntario_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_voluntario_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS voluntario_ministerio (
  voluntario_id BIGINT UNSIGNED NOT NULL,
  ministerio_id BIGINT UNSIGNED NOT NULL,
  principal TINYINT(1) NOT NULL DEFAULT 0,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (voluntario_id, ministerio_id),
  KEY ix_voluntario_ministerio_ministerio (ministerio_id),
  CONSTRAINT fk_voluntario_ministerio_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE CASCADE,
  CONSTRAINT fk_voluntario_ministerio_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

-- =========================
-- CULTOS, TEMPLATES E ETAPAS
-- =========================

CREATE TABLE IF NOT EXISTS status_culto (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome VARCHAR(80) NOT NULL,
  codigo VARCHAR(50) NOT NULL,
  cor_hex VARCHAR(7) NULL,
  ordem INT NOT NULL DEFAULT 0,
  PRIMARY KEY (id),
  UNIQUE KEY uq_status_culto_codigo (codigo),
  UNIQUE KEY uq_status_culto_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS status_etapa (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome VARCHAR(80) NOT NULL,
  codigo VARCHAR(50) NOT NULL,
  cor_hex VARCHAR(7) NULL,
  ordem INT NOT NULL DEFAULT 0,
  PRIMARY KEY (id),
  UNIQUE KEY uq_status_etapa_codigo (codigo),
  UNIQUE KEY uq_status_etapa_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS template_culto (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome VARCHAR(150) NOT NULL,
  descricao VARCHAR(500) NULL,
  tipo_culto VARCHAR(80) NOT NULL,
  ativo TINYINT(1) NOT NULL DEFAULT 1,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_template_culto_ativo (ativo),
  KEY ix_template_culto_tipo (tipo_culto),
  CONSTRAINT fk_template_culto_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_template_culto_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS template_etapa_culto (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  template_culto_id BIGINT UNSIGNED NOT NULL,
  sequencia INT NOT NULL,
  horario_inicial_padrao TIME NULL,
  duracao_minutos INT NOT NULL,
  atividade VARCHAR(150) NOT NULL,
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

CREATE TABLE IF NOT EXISTS template_etapa_culto_ministerio (
  template_etapa_culto_id BIGINT UNSIGNED NOT NULL,
  ministerio_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (template_etapa_culto_id, ministerio_id),
  KEY ix_template_etapa_ministerio_ministerio (ministerio_id),
  CONSTRAINT fk_template_etapa_ministerio_link_etapa FOREIGN KEY (template_etapa_culto_id) REFERENCES template_etapa_culto(id) ON DELETE CASCADE,
  CONSTRAINT fk_template_etapa_ministerio_link_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS template_checklist_item (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  template_culto_id BIGINT UNSIGNED NOT NULL,
  ministerio_id BIGINT UNSIGNED NULL,
  fase ENUM('PRE_CULTO','DURANTE_CULTO','POS_CULTO') NOT NULL,
  descricao VARCHAR(250) NOT NULL,
  obrigatorio TINYINT(1) NOT NULL DEFAULT 0,
  ordem INT NOT NULL DEFAULT 0,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_template_checklist_template (template_culto_id),
  KEY ix_template_checklist_fase (fase),
  KEY ix_template_checklist_ministerio (ministerio_id),
  CONSTRAINT fk_template_checklist_template FOREIGN KEY (template_culto_id) REFERENCES template_culto(id) ON DELETE CASCADE,
  CONSTRAINT fk_template_checklist_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS culto (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  template_culto_id BIGINT UNSIGNED NULL,
  culto_origem_id BIGINT UNSIGNED NULL,
  nome VARCHAR(150) NOT NULL,
  tipo_culto VARCHAR(80) NOT NULL,
  data_culto DATE NOT NULL,
  horario_inicio TIME NOT NULL,
  horario_fim_previsto TIME NULL,
  status_culto_id BIGINT UNSIGNED NOT NULL,
  observacoes_gerais VARCHAR(800) NULL,
  total_visitantes INT NOT NULL DEFAULT 0,
  total_novos_convertidos INT NOT NULL DEFAULT 0,
  ativo TINYINT(1) NOT NULL DEFAULT 1,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_culto_data (data_culto),
  KEY ix_culto_status (status_culto_id),
  KEY ix_culto_tipo (tipo_culto),
  KEY ix_culto_template (template_culto_id),
  CONSTRAINT fk_culto_template FOREIGN KEY (template_culto_id) REFERENCES template_culto(id) ON DELETE SET NULL,
  CONSTRAINT fk_culto_origem FOREIGN KEY (culto_origem_id) REFERENCES culto(id) ON DELETE SET NULL,
  CONSTRAINT fk_culto_status FOREIGN KEY (status_culto_id) REFERENCES status_culto(id) ON DELETE RESTRICT,
  CONSTRAINT fk_culto_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_culto_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS etapa_culto (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  culto_id BIGINT UNSIGNED NOT NULL,
  sequencia INT NOT NULL,
  horario_inicio DATETIME NOT NULL,
  duracao_minutos INT NOT NULL,
  horario_fim_calculado DATETIME NULL,
  horario_conclusao_real DATETIME NULL,
  atividade VARCHAR(150) NOT NULL,
  descricao VARCHAR(500) NULL,
  responsavel_principal_usuario_id BIGINT UNSIGNED NULL,
  ministerio_responsavel_id BIGINT UNSIGNED NULL,
  observacoes VARCHAR(500) NULL,
  atraso_minutos INT NOT NULL DEFAULT 0,
  status_etapa_id BIGINT UNSIGNED NOT NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_etapa_culto_culto (culto_id),
  KEY ix_etapa_culto_sequencia (sequencia),
  KEY ix_etapa_culto_status (status_etapa_id),
  KEY ix_etapa_culto_horario_inicio (horario_inicio),
  KEY ix_etapa_culto_ministerio (ministerio_responsavel_id),
  CONSTRAINT fk_etapa_culto_culto FOREIGN KEY (culto_id) REFERENCES culto(id) ON DELETE CASCADE,
  CONSTRAINT fk_etapa_culto_responsavel FOREIGN KEY (responsavel_principal_usuario_id) REFERENCES usuario(id) ON DELETE SET NULL,
  CONSTRAINT fk_etapa_culto_ministerio FOREIGN KEY (ministerio_responsavel_id) REFERENCES ministerio(id) ON DELETE SET NULL,
  CONSTRAINT fk_etapa_culto_status FOREIGN KEY (status_etapa_id) REFERENCES status_etapa(id) ON DELETE RESTRICT,
  CONSTRAINT fk_etapa_culto_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_etapa_culto_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

-- =========================
-- ESCALA, PRESENCA E CHECKLIST
-- =========================

CREATE TABLE IF NOT EXISTS presenca_escala_status (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome VARCHAR(80) NOT NULL,
  codigo VARCHAR(50) NOT NULL,
  cor_hex VARCHAR(7) NULL,
  ordem INT NOT NULL DEFAULT 0,
  PRIMARY KEY (id),
  UNIQUE KEY uq_presenca_status_codigo (codigo),
  UNIQUE KEY uq_presenca_status_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

CREATE TABLE IF NOT EXISTS escala (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  culto_id BIGINT UNSIGNED NOT NULL,
  etapa_culto_id BIGINT UNSIGNED NULL,
  voluntario_id BIGINT UNSIGNED NOT NULL,
  ministerio_id BIGINT UNSIGNED NULL,
  funcao VARCHAR(120) NOT NULL,
  horario_previsto DATETIME NULL,
  presenca_status_id BIGINT UNSIGNED NOT NULL,
  confirmado_em DATETIME NULL,
  substituido_por_voluntario_id BIGINT UNSIGNED NULL,
  observacoes VARCHAR(500) NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_escala_culto (culto_id),
  KEY ix_escala_etapa (etapa_culto_id),
  KEY ix_escala_voluntario (voluntario_id),
  KEY ix_escala_status (presenca_status_id),
  KEY ix_escala_ministerio (ministerio_id),
  KEY ix_escala_funcao (funcao),
  CONSTRAINT fk_escala_culto FOREIGN KEY (culto_id) REFERENCES culto(id) ON DELETE CASCADE,
  CONSTRAINT fk_escala_etapa FOREIGN KEY (etapa_culto_id) REFERENCES etapa_culto(id) ON DELETE SET NULL,
  CONSTRAINT fk_escala_voluntario FOREIGN KEY (voluntario_id) REFERENCES voluntario(id) ON DELETE RESTRICT,
  CONSTRAINT fk_escala_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE SET NULL,
  CONSTRAINT fk_escala_status FOREIGN KEY (presenca_status_id) REFERENCES presenca_escala_status(id) ON DELETE RESTRICT,
  CONSTRAINT fk_escala_substituido_por FOREIGN KEY (substituido_por_voluntario_id) REFERENCES voluntario(id) ON DELETE SET NULL,
  CONSTRAINT fk_escala_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_escala_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

CREATE TABLE IF NOT EXISTS presenca_escala (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  escala_id BIGINT UNSIGNED NOT NULL,
  presenca_status_id BIGINT UNSIGNED NOT NULL,
  confirmado TINYINT(1) NOT NULL DEFAULT 0,
  confirmado_em DATETIME NULL,
  justificativa VARCHAR(500) NULL,
  registrado_por BIGINT UNSIGNED NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_presenca_escala_escala (escala_id),
  KEY ix_presenca_escala_status (presenca_status_id),
  KEY ix_presenca_escala_confirmado_em (confirmado_em),
  CONSTRAINT fk_presenca_escala_escala FOREIGN KEY (escala_id) REFERENCES escala(id) ON DELETE CASCADE,
  CONSTRAINT fk_presenca_escala_status FOREIGN KEY (presenca_status_id) REFERENCES presenca_escala_status(id) ON DELETE RESTRICT,
  CONSTRAINT fk_presenca_escala_registrado_por FOREIGN KEY (registrado_por) REFERENCES usuario(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

CREATE TABLE IF NOT EXISTS disponibilidade_culto_voluntario_ministerio (
  disponibilidade_culto_voluntario_id BIGINT UNSIGNED NOT NULL,
  ministerio_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (disponibilidade_culto_voluntario_id, ministerio_id),
  KEY ix_disp_ministerio_ministerio (ministerio_id),
  CONSTRAINT fk_disp_ministerio_disp FOREIGN KEY (disponibilidade_culto_voluntario_id) REFERENCES disponibilidade_culto_voluntario(id) ON DELETE CASCADE,
  CONSTRAINT fk_disp_ministerio_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS checklist (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  culto_id BIGINT UNSIGNED NOT NULL,
  ministerio_id BIGINT UNSIGNED NULL,
  fase ENUM('PRE_CULTO','DURANTE_CULTO','POS_CULTO') NOT NULL,
  titulo VARCHAR(150) NOT NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_checklist_culto (culto_id),
  KEY ix_checklist_ministerio (ministerio_id),
  KEY ix_checklist_fase (fase),
  CONSTRAINT fk_checklist_culto FOREIGN KEY (culto_id) REFERENCES culto(id) ON DELETE CASCADE,
  CONSTRAINT fk_checklist_ministerio FOREIGN KEY (ministerio_id) REFERENCES ministerio(id) ON DELETE SET NULL,
  CONSTRAINT fk_checklist_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_checklist_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS checklist_item (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  checklist_id BIGINT UNSIGNED NOT NULL,
  descricao VARCHAR(250) NOT NULL,
  ordem INT NOT NULL DEFAULT 0,
  status_item ENUM('PENDENTE','CONCLUIDO','NAO_APLICA') NOT NULL DEFAULT 'PENDENTE',
  concluido_em DATETIME NULL,
  concluido_por BIGINT UNSIGNED NULL,
  observacoes VARCHAR(500) NULL,
  PRIMARY KEY (id),
  KEY ix_checklist_item_checklist (checklist_id),
  KEY ix_checklist_item_status (status_item),
  KEY ix_checklist_item_ordem (ordem),
  CONSTRAINT fk_checklist_item_checklist FOREIGN KEY (checklist_id) REFERENCES checklist(id) ON DELETE CASCADE,
  CONSTRAINT fk_checklist_item_concluido_por FOREIGN KEY (concluido_por) REFERENCES usuario(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================
-- CONVIDADOS E CONVERTIDOS
-- =========================

CREATE TABLE IF NOT EXISTS convidado (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  culto_id BIGINT UNSIGNED NOT NULL,
  nome VARCHAR(150) NOT NULL,
  telefone VARCHAR(30) NULL,
  quem_convidou VARCHAR(150) NULL,
  primeira_vez_igreja TINYINT(1) NOT NULL DEFAULT 1,
  observacoes VARCHAR(500) NULL,
  status_acompanhamento ENUM('PENDENTE','EM_ANDAMENTO','CONCLUIDO') NOT NULL DEFAULT 'PENDENTE',
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_convidado_culto (culto_id),
  KEY ix_convidado_status (status_acompanhamento),
  KEY ix_convidado_nome (nome),
  CONSTRAINT fk_convidado_culto FOREIGN KEY (culto_id) REFERENCES culto(id) ON DELETE CASCADE,
  CONSTRAINT fk_convidado_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_convidado_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS novo_convertido (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  convidado_id BIGINT UNSIGNED NOT NULL,
  data_decisao DATE NULL,
  status_acompanhamento ENUM('PENDENTE','EM_ANDAMENTO','CONCLUIDO') NOT NULL DEFAULT 'PENDENTE',
  observacoes VARCHAR(500) NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME NULL,
  criado_por BIGINT UNSIGNED NULL,
  atualizado_por BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_novo_convertido_convidado (convidado_id),
  KEY ix_novo_convertido_status (status_acompanhamento),
  CONSTRAINT fk_novo_convertido_convidado FOREIGN KEY (convidado_id) REFERENCES convidado(id) ON DELETE CASCADE,
  CONSTRAINT fk_novo_convertido_criado_por FOREIGN KEY (criado_por) REFERENCES usuario(id),
  CONSTRAINT fk_novo_convertido_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES usuario(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================
-- AUDITORIA
-- =========================

CREATE TABLE IF NOT EXISTS auditoria (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  tabela VARCHAR(100) NOT NULL,
  entidade_id VARCHAR(50) NOT NULL,
  acao ENUM('CREATE','UPDATE','DELETE','LOGIN','LOGOUT') NOT NULL,
  dados_anteriores TEXT NULL,
  dados_novos TEXT NULL,
  usuario_id BIGINT UNSIGNED NULL,
  ip_origem VARCHAR(45) NULL,
  user_agent VARCHAR(255) NULL,
  criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_auditoria_tabela_entidade (tabela, entidade_id),
  KEY ix_auditoria_usuario (usuario_id),
  KEY ix_auditoria_acao (acao),
  KEY ix_auditoria_criado_em (criado_em),
  CONSTRAINT fk_auditoria_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================
-- DADOS INICIAIS (SEEDS)
-- =========================

INSERT INTO perfil (nome, codigo, ativo)
VALUES
  ('Administrador', 'ADMIN', 1),
  ('Gestão de Culto', 'GESTAO_CULTO', 1),
  ('Líder de Ministério', 'LIDER_MINISTERIO', 1),
  ('Voluntário', 'VOLUNTARIO', 1),
  ('Recepção / Dados', 'RECEPCAO_DADOS', 1)
ON DUPLICATE KEY UPDATE nome = VALUES(nome), ativo = VALUES(ativo);

INSERT INTO ministerio (nome, codigo, ativo)
VALUES
  ('Louvor', 'LOUVOR', 1),
  ('Recepção', 'RECEPCAO', 1),
  ('Dados', 'DADOS', 1),
  ('Atmosfera', 'ATMOSFERA', 1),
  ('Iluminação', 'ILUMINACAO', 1),
  ('Telão', 'TELAO', 1),
  ('Som', 'SOM', 1),
  ('Produção', 'PRODUCAO', 1),
  ('Kids', 'KIDS', 1),
  ('Oferta', 'OFERTA', 1),
  ('Estacionamento', 'ESTACIONAMENTO', 1),
  ('Café', 'CAFE', 1),
  ('Púlpito', 'PULPITO', 1)
ON DUPLICATE KEY UPDATE nome = VALUES(nome), ativo = VALUES(ativo);

INSERT INTO status_culto (nome, codigo, cor_hex, ordem)
VALUES
  ('Planejamento', 'PLANEJAMENTO', '#6C757D', 1),
  ('Fechado', 'FECHADO', '#0D6EFD', 2),
  ('Em andamento', 'EM_ANDAMENTO', '#FD7E14', 3),
  ('Finalizado', 'FINALIZADO', '#198754', 4),
  ('Cancelado', 'CANCELADO', '#DC3545', 5)
ON DUPLICATE KEY UPDATE nome = VALUES(nome), cor_hex = VALUES(cor_hex), ordem = VALUES(ordem);

INSERT INTO status_etapa (nome, codigo, cor_hex, ordem)
VALUES
  ('Pendente', 'PENDENTE', '#6C757D', 1),
  ('Em andamento', 'EM_ANDAMENTO', '#FD7E14', 2),
  ('Concluída', 'CONCLUIDA', '#198754', 3),
  ('Atrasada', 'ATRASADA', '#DC3545', 4),
  ('Cancelada', 'CANCELADA', '#212529', 5)
ON DUPLICATE KEY UPDATE nome = VALUES(nome), cor_hex = VALUES(cor_hex), ordem = VALUES(ordem);

INSERT INTO presenca_escala_status (nome, codigo, cor_hex, ordem)
VALUES
  ('Pendente', 'PENDENTE', '#6C757D', 1),
  ('Confirmado', 'CONFIRMADO', '#198754', 2),
  ('Ausente', 'AUSENTE', '#DC3545', 3),
  ('Substituído', 'SUBSTITUIDO', '#FD7E14', 4)
ON DUPLICATE KEY UPDATE nome = VALUES(nome), cor_hex = VALUES(cor_hex), ordem = VALUES(ordem);

INSERT INTO status_disponibilidade_voluntario (nome, codigo, cor_hex, ordem)
VALUES
  ('Disponível', 'DISPONIVEL', '#198754', 1),
  ('Indisponível', 'INDISPONIVEL', '#DC3545', 2),
  ('Em análise', 'EM_ANALISE', '#FD7E14', 3)
ON DUPLICATE KEY UPDATE nome = VALUES(nome), cor_hex = VALUES(cor_hex), ordem = VALUES(ordem);

SET FOREIGN_KEY_CHECKS = 1;
