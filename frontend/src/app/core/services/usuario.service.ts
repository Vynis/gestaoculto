import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Usuario, UsuarioOpcoes, UsuarioRequest } from '../models/usuario.models';

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private readonly apiUrl = `${environment.apiUrl}/usuarios`;

  constructor(private readonly http: HttpClient) {}

  listar(busca?: string): Observable<Usuario[]> {
    const params = busca ? new HttpParams().set('busca', busca) : undefined;
    return this.http.get<Usuario[]>(this.apiUrl, { params });
  }

  opcoes(): Observable<UsuarioOpcoes> {
    return this.http.get<UsuarioOpcoes>(`${this.apiUrl}/opcoes`);
  }

  criar(payload: UsuarioRequest): Observable<{ mensagem: string; id: number }> {
    return this.http.post<{ mensagem: string; id: number }>(this.apiUrl, payload);
  }

  atualizar(id: number, payload: UsuarioRequest): Observable<{ mensagem: string }> {
    return this.http.put<{ mensagem: string }>(`${this.apiUrl}/${id}`, payload);
  }

  excluir(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/${id}`);
  }

  resetarSenha(id: number): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/${id}/resetar-senha`, {});
  }
}
