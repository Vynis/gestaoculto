import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardEstatisticas, DashboardResumo } from '../models/dashboard.models';
import { HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly apiUrl = `${environment.apiUrl}/dashboard`;

  constructor(private readonly http: HttpClient) {}

  obterResumo(): Observable<DashboardResumo> {
    return this.http.get<DashboardResumo>(`${this.apiUrl}/resumo`);
  }

  obterEstatisticas(inicio: string, fim: string, limite = 5): Observable<DashboardEstatisticas> {
    const params = new HttpParams()
      .set('inicio', inicio)
      .set('fim', fim)
      .set('limite', limite);

    return this.http.get<DashboardEstatisticas>(`${this.apiUrl}/estatisticas`, { params });
  }
}
