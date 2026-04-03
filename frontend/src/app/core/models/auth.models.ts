export interface AuthResponse {
  token: string;
  expiraEm: string;
  nome: string;
  email: string;
  perfis: string[];
}

export interface AuthSession {
  token: string;
  nome: string;
  email: string;
  perfis: string[];
}
