import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RepertorioCulto } from '../models/repertorio.models';

@Injectable({ providedIn: 'root' })
export class RepertorioService {
  private readonly apiUrl = `${environment.apiUrl}/repertorios`;

  constructor(private readonly http: HttpClient) {}

  obterPorCulto(cultoId: number): Observable<RepertorioCulto> {
    return this.http.get<RepertorioCulto>(`${this.apiUrl}/culto/${cultoId}`);
  }

  salvar(payload: RepertorioCulto): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(this.apiUrl, payload);
  }

  duplicarDoCultoAnterior(cultoId: number): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/duplicar-culto-anterior`, cultoId);
  }
}
