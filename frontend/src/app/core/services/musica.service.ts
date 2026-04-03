import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Musica, MusicaRequest } from '../models/musica.models';

@Injectable({ providedIn: 'root' })
export class MusicaService {
  private readonly apiUrl = `${environment.apiUrl}/musicas`;

  constructor(private readonly http: HttpClient) {}

  listar(busca?: string, ativo?: boolean): Observable<Musica[]> {
    let params = new HttpParams();
    if (busca) {
      params = params.set('busca', busca);
    }
    if (typeof ativo === 'boolean') {
      params = params.set('ativo', `${ativo}`);
    }

    return this.http.get<Musica[]>(this.apiUrl, { params });
  }

  criar(payload: MusicaRequest): Observable<{ mensagem: string; id: number }> {
    return this.http.post<{ mensagem: string; id: number }>(this.apiUrl, payload);
  }

  atualizar(id: number, payload: MusicaRequest): Observable<{ mensagem: string }> {
    return this.http.put<{ mensagem: string }>(`${this.apiUrl}/${id}`, payload);
  }

  excluir(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/${id}`);
  }
}
