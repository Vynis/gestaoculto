import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RelatorioCultoDiaResponse } from '../models/relatorio-culto.models';
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

  listarCultosParaRelatorio(): Observable<Culto[]> {
    return this.http.get<Culto[]>(`${this.apiUrl}/cultos`);
  }
}
