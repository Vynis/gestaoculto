import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  RelatorioCompartilhadoLinkDto,
  RelatorioCultoDiaResponse,
  RelatorioEscalaMensalResponse
} from '../models/relatorio-culto.models';
import { Culto } from '../models/culto.models';

@Injectable({ providedIn: 'root' })
export class RelatorioService {
  private readonly apiUrl = `${environment.apiUrl}/relatorios`;

  constructor(private readonly http: HttpClient) {}

  obterRelatorioCultoDia(data: string): Observable<RelatorioCultoDiaResponse> {
    const params = new HttpParams().set('data', data);
    return this.http.get<RelatorioCultoDiaResponse>(`${this.apiUrl}/culto-dia`, { params });
  }

  obterRelatorioCultoPorId(cultoId: number): Observable<RelatorioCultoDiaResponse> {
    return this.http.get<RelatorioCultoDiaResponse>(`${this.apiUrl}/culto/${cultoId}`);
  }

  gerarLinkRelatorioCompartilhado(cultoId: number): Observable<RelatorioCompartilhadoLinkDto> {
    return this.http.post<RelatorioCompartilhadoLinkDto>(`${this.apiUrl}/compartilhar/${cultoId}`, {});
  }

  obterRelatorioCompartilhado(token: string): Observable<RelatorioCultoDiaResponse> {
    const params = new HttpParams().set('t', token);
    const apiUrl = !environment.production && !['localhost', '127.0.0.1'].includes(window.location.hostname)
      ? `${window.location.origin}/api/relatorios`
      : this.apiUrl;
    return this.http.get<RelatorioCultoDiaResponse>(`${apiUrl}/compartilhado`, { params });
  }

  listarCultosParaRelatorio(): Observable<Culto[]> {
    return this.http.get<Culto[]>(`${this.apiUrl}/cultos`);
  }

  obterRelatorioEscalaMensal(
    mes: string,
    ministerioId?: number | null,
    voluntarioId?: number | null,
    presencaStatusId?: number | null
  ): Observable<RelatorioEscalaMensalResponse> {
    let params = new HttpParams().set('mes', mes);
    if (ministerioId && ministerioId > 0) {
      params = params.set('ministerioId', String(ministerioId));
    }
    if (voluntarioId && voluntarioId > 0) {
      params = params.set('voluntarioId', String(voluntarioId));
    }
    if (presencaStatusId && presencaStatusId > 0) {
      params = params.set('presencaStatusId', String(presencaStatusId));
    }
    return this.http.get<RelatorioEscalaMensalResponse>(`${this.apiUrl}/escala-mensal`, { params });
  }
}
