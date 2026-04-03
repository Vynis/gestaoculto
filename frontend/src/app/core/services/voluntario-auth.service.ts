import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class VoluntarioAuthService {
  private readonly apiUrl = `${environment.apiUrl}/voluntario-auth`;

  constructor(private readonly http: HttpClient) {}

  solicitarAtivacao(identificador: string, canal = 'APP'): Observable<{ mensagem: string; codigo: string; expiraEm: string }> {
    return this.http.post<{ mensagem: string; codigo: string; expiraEm: string }>(`${this.apiUrl}/solicitar-ativacao`, {
      identificador,
      canal
    });
  }

  ativarAcesso(payload: {
    identificador: string;
    codigo: string;
    senha: string;
    manterConectado: boolean;
    nome?: string;
    email?: string;
    telefone?: string;
  }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/ativar-acesso`, payload);
  }

  solicitarRecuperacao(identificador: string, canal = 'APP'): Observable<{ mensagem: string; codigo: string; expiraEm: string }> {
    return this.http.post<{ mensagem: string; codigo: string; expiraEm: string }>(`${this.apiUrl}/solicitar-recuperacao`, {
      identificador,
      canal
    });
  }

  redefinirAcesso(payload: { identificador: string; codigo: string; novaSenha: string }): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/redefinir-acesso`, payload);
  }
}
