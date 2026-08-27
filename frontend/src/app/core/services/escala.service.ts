import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Escala } from '../models/escala.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class EscalaService {
  private readonly apiUrl = `${environment.apiUrl}/escalas`;

  constructor(private readonly http: HttpClient) {}

  listarPorCulto(cultoId: number): Observable<Escala[]> {
    return this.http.get<Escala[]>(`${this.apiUrl}/culto/${cultoId}`);
  }

  criar(payload: Partial<Escala>): Observable<Escala> {
    return this.http.post<Escala>(this.apiUrl, payload);
  }

  atualizar(id: number, payload: Partial<Escala>): Observable<Escala> {
    return this.http.put<Escala>(`${this.apiUrl}/${id}`, payload);
  }

  confirmar(id: number): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  cancelarConfirmacao(id: number): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/${id}/cancelar-confirmacao`, {});
  }

  confirmarTodasPorCulto(cultoId: number, ministerioId?: number | null): Observable<{ mensagem: string; total: number }> {
    let params = new HttpParams().set('cultoId', String(cultoId));
    if (ministerioId && ministerioId > 0) {
      params = params.set('ministerioId', String(ministerioId));
    }

    return this.http.post<{ mensagem: string; total: number }>(`${this.apiUrl}/confirmar-todas`, {}, { params });
  }

  excluir(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/${id}`);
  }

  excluirPorCulto(cultoId: number): Observable<{ mensagem: string; removidas: number }> {
    return this.http.delete<{ mensagem: string; removidas: number }>(`${this.apiUrl}/culto/${cultoId}`);
  }
}
