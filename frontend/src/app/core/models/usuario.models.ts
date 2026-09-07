export interface PerfilOpcao {
  id: number;
  nome: string;
  codigo: string;
}

export interface Usuario {
  id: number;
  nome: string;
  email: string;
  telefone: string | null;
  ativo: boolean;
  deveTrocarSenha: boolean;
  perfis: string[];
  perfilIds: number[];
}

export interface UsuarioRequest {
  nome: string;
  email: string;
  telefone: string | null;
  ativo: boolean;
  senha: string | null;
  perfilIds: number[];
}

export interface UsuarioOpcoes {
  perfis: PerfilOpcao[];
}
