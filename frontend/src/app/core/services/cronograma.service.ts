import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EtapaCulto } from '../models/cronograma.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CronogramaService {
  private readonly apiUrl = `${environment.apiUrl}/cronograma`;

  constructor(private readonly http: HttpClient) {}

  listarPorCulto(cultoId: number): Observable<EtapaCulto[]> {
    return this.http.get<EtapaCulto[]>(`${this.apiUrl}/culto/${cultoId}`);
  }

  criar(payload: Partial<EtapaCulto>): Observable<EtapaCulto> {
    return this.http.post<EtapaCulto>(this.apiUrl, payload);
  }

  atualizar(id: number, payload: Partial<EtapaCulto>): Observable<EtapaCulto> {
    return this.http.put<EtapaCulto>(`${this.apiUrl}/${id}`, payload);
  }

  excluir(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/${id}`);
  }

  excluirTodasDoCulto(cultoId: number): Observable<{ mensagem: string; removidas: number }> {
    return this.http.delete<{ mensagem: string; removidas: number }>(`${this.apiUrl}/culto/${cultoId}`);
  }
}
