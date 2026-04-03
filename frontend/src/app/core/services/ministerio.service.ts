import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MinisterioCompleto, MinisterioOpcoes, MinisterioRequest } from '../models/ministerio.models';

@Injectable({ providedIn: 'root' })
export class MinisterioService {
  private readonly apiUrl = `${environment.apiUrl}/ministerios`;

  constructor(private readonly http: HttpClient) {}

  listar(busca?: string): Observable<MinisterioCompleto[]> {
    const params = busca ? new HttpParams().set('busca', busca) : undefined;
    return this.http.get<MinisterioCompleto[]>(this.apiUrl, { params });
  }

  opcoes(): Observable<MinisterioOpcoes> {
    return this.http.get<MinisterioOpcoes>(`${this.apiUrl}/opcoes`);
  }

  criar(payload: MinisterioRequest): Observable<{ mensagem: string; id: number }> {
    return this.http.post<{ mensagem: string; id: number }>(this.apiUrl, payload);
  }

  atualizar(id: number, payload: MinisterioRequest): Observable<{ mensagem: string }> {
    return this.http.put<{ mensagem: string }>(`${this.apiUrl}/${id}`, payload);
  }

  excluir(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/${id}`);
  }
}
