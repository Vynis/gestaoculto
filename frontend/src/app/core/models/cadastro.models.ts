export interface Ministerio {
  id: number;
  nome: string;
  codigo: string;
  descricao: string | null;
  ativo: boolean;
  funcoesPadrao?: MinisterioFuncaoPadrao[];
}

export interface MinisterioFuncaoPadrao {
  id?: number;
  ministerioId?: number;
  nome: string;
  ordem?: number;
  ativo: boolean;
}

export interface Voluntario {
  id: number;
  usuarioId: number | null;
  usuarioNome?: string | null;
  nome: string;
  telefone: string | null;
  email: string | null;
  ministerioPrincipalId: number | null;
  ministerioIds: number[];
  observacoes: string | null;
  restricoesIndisponibilidade: string | null;
  ativo: boolean;
}

export interface UsuarioOpcaoVoluntario {
  id: number;
  nome: string;
  email: string;
  voluntarioId: number | null;
}

export interface TelegramVinculo {
  url: string;
  expiraEm: string;
}

export interface TelegramConexaoStatus {
  vinculado: boolean;
  ativo: boolean;
  username: string | null;
  vinculadoEm: string | null;
  ultimaInteracaoEm: string | null;
}
