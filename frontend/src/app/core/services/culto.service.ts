import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { Culto, CultoRecorrencia, CultoRecorrenciaGeracaoResponse, CultoRecorrenciaRequest, CultoRequest } from '../models/culto.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CultoService {
  private readonly apiUrl = `${environment.apiUrl}/cultos`;
  private readonly recorrenciasUrl = `${environment.apiUrl}/cultos-recorrencias`;
  static readonly STATUS_CULTO_ATIVO_ID = 1;

  constructor(private readonly http: HttpClient) {}

  static ordenarPorProximidade(cultos: Culto[]): Culto[] {
    const hoje = new Date();
    hoje.setHours(0, 0, 0, 0);
    const inicioHoje = hoje.getTime();

    return (cultos || []).slice().sort((a, b) => {
      const dataA = CultoService.dataHoraCulto(a).getTime();
      const dataB = CultoService.dataHoraCulto(b).getTime();
      const futuroA = dataA >= inicioHoje;
      const futuroB = dataB >= inicioHoje;

      if (futuroA !== futuroB) {
        return futuroA ? -1 : 1;
      }

      return futuroA ? dataA - dataB : dataB - dataA;
    });
  }

  listar(): Observable<Culto[]> {
    return this.http.get<Culto[]>(this.apiUrl);
  }

  listarAtivos(): Observable<Culto[]> {
    return this.listar().pipe(
      map((cultos) => CultoService.ordenarPorProximidade(
        cultos.filter((culto) => Number(culto.statusCultoId) === CultoService.STATUS_CULTO_ATIVO_ID)
      ))
    );
  }

  criar(dto: CultoRequest): Observable<Culto> {
    return this.http.post<Culto>(this.apiUrl, dto);
  }

  atualizar(id: number, dto: CultoRequest): Observable<Culto> {
    return this.http.put<Culto>(`${this.apiUrl}/${id}`, dto);
  }

  excluir(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/${id}`);
  }

  listarRecorrencias(): Observable<CultoRecorrencia[]> {
    return this.http.get<CultoRecorrencia[]>(this.recorrenciasUrl);
  }

  criarRecorrencia(dto: CultoRecorrenciaRequest): Observable<CultoRecorrencia> {
    return this.http.post<CultoRecorrencia>(this.recorrenciasUrl, dto);
  }

  atualizarRecorrencia(id: number, dto: CultoRecorrenciaRequest): Observable<CultoRecorrencia> {
    return this.http.put<CultoRecorrencia>(`${this.recorrenciasUrl}/${id}`, dto);
  }

  excluirRecorrencia(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.recorrenciasUrl}/${id}`);
  }

  gerarCultosRecorrencia(id: number): Observable<CultoRecorrenciaGeracaoResponse> {
    return this.http.post<CultoRecorrenciaGeracaoResponse>(`${this.recorrenciasUrl}/${id}/gerar`, {});
  }

  private static dataHoraCulto(culto: Culto): Date {
    const dataTexto = String(culto.dataCulto || '').slice(0, 10);
    const horarioTexto = CultoService.horarioTexto(culto.horarioInicio);
    const data = new Date(`${dataTexto}T${horarioTexto}`);

    if (!Number.isNaN(data.getTime())) {
      return data;
    }

    const fallback = new Date(culto.dataCulto || 0);
    return Number.isNaN(fallback.getTime()) ? new Date(0) : fallback;
  }

  private static horarioTexto(valor: unknown): string {
    if (!valor) {
      return '00:00:00';
    }

    if (typeof valor === 'object') {
      const origem = valor as Record<string, unknown>;
      const hora = Number(origem['hours'] ?? origem['hour'] ?? origem['Hours'] ?? origem['Hour']);
      const minuto = Number(origem['minutes'] ?? origem['minute'] ?? origem['Minutes'] ?? origem['Minute']);

      if (!Number.isNaN(hora) && !Number.isNaN(minuto)) {
        return `${`${hora}`.padStart(2, '0')}:${`${minuto}`.padStart(2, '0')}:00`;
      }
    }

    const texto = String(valor).trim();
    const match = texto.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?/);
    if (!match) {
      return '00:00:00';
    }

    return `${match[1].padStart(2, '0')}:${match[2]}:${match[3] ?? '00'}`;
  }
}
