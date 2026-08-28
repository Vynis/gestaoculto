import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DisponibilidadeGestaoItem,
  StatusDisponibilidadeVoluntario,
  VoluntarioCalendarioResponse,
  VoluntarioCultosPlanejadosResponse,
  VoluntarioDisponibilidadeCultoDetalhe,
  VoluntarioColegaMinisterio,
  VoluntarioCompromisso,
  VoluntarioGoogleCalendarItem,
  VoluntarioGoogleCalendarStatus,
  VoluntarioEscalaDetalheResponse,
  VoluntarioMeusDados,
  VoluntarioMinisterioResumo,
  VoluntarioRepertorioDetalhe,
  VoluntarioRepertorioListaItem,
  VoluntarioPainelResponse
} from '../models/portal-voluntario.models';
import { RepertorioCulto } from '../models/repertorio.models';
import { Musica } from '../models/musica.models';

@Injectable({ providedIn: 'root' })
export class PortalVoluntarioService {
  private readonly apiUrl = `${environment.apiUrl}/portal-voluntario`;

  constructor(private readonly http: HttpClient) {}

  obterPainel(): Observable<VoluntarioPainelResponse> {
    return this.http.get<VoluntarioPainelResponse>(`${this.apiUrl}/painel`);
  }

  obterCalendario(ano: number, mes: number): Observable<VoluntarioCalendarioResponse> {
    return this.http.get<VoluntarioCalendarioResponse>(`${this.apiUrl}/calendario?ano=${ano}&mes=${mes}`);
  }

  listarMinhaEscala(inicio?: string, fim?: string): Observable<VoluntarioCompromisso[]> {
    const params = [`inicio=${inicio || ''}`, `fim=${fim || ''}`].join('&');
    return this.http.get<VoluntarioCompromisso[]>(`${this.apiUrl}/minha-escala?${params}`);
  }

  obterDetalheEscala(id: number): Observable<VoluntarioEscalaDetalheResponse> {
    return this.http.get<VoluntarioEscalaDetalheResponse>(`${this.apiUrl}/minha-escala/${id}/detalhe`);
  }

  obterRepertorioDaEscala(escalaId: number): Observable<RepertorioCulto> {
    return this.http.get<RepertorioCulto>(`${this.apiUrl}/escala/${escalaId}/repertorio`);
  }

  listarMusicasDaEscala(escalaId: number, busca?: string): Observable<Musica[]> {
    const query = busca ? `?busca=${encodeURIComponent(busca)}` : '';
    return this.http.get<Musica[]>(`${this.apiUrl}/escala/${escalaId}/musicas${query}`);
  }

  salvarRepertorioDaEscala(escalaId: number, payload: RepertorioCulto): Observable<{ mensagem: string }> {
    return this.http.put<{ mensagem: string }>(`${this.apiUrl}/escala/${escalaId}/repertorio`, payload);
  }

  cadastrarMusicaDaEscala(escalaId: number, payload: Partial<Musica>): Observable<Musica> {
    return this.http.post<Musica>(`${this.apiUrl}/escala/${escalaId}/musicas`, payload);
  }

  confirmarPresenca(id: number): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/minha-escala/${id}/confirmar`, {});
  }

  listarMeusMinisterios(): Observable<VoluntarioMinisterioResumo[]> {
    return this.http.get<VoluntarioMinisterioResumo[]>(`${this.apiUrl}/meus-ministerios`);
  }

  listarRepertorios(): Observable<VoluntarioRepertorioListaItem[]> {
    return this.http.get<VoluntarioRepertorioListaItem[]>(`${this.apiUrl}/repertorios`);
  }

  obterRepertorioCulto(cultoId: number): Observable<VoluntarioRepertorioDetalhe> {
    return this.http.get<VoluntarioRepertorioDetalhe>(`${this.apiUrl}/repertorios/culto/${cultoId}`);
  }

  listarMusicasLouvor(busca?: string): Observable<Musica[]> {
    const query = busca ? `?busca=${encodeURIComponent(busca)}` : '';
    return this.http.get<Musica[]>(`${this.apiUrl}/repertorios/musicas${query}`);
  }

  listarColegasMinisterio(): Observable<VoluntarioColegaMinisterio[]> {
    return this.http.get<VoluntarioColegaMinisterio[]>(`${this.apiUrl}/colegas-ministerio`);
  }

  obterMeusDados(): Observable<VoluntarioMeusDados> {
    return this.http.get<VoluntarioMeusDados>(`${this.apiUrl}/meus-dados`);
  }

  atualizarMeusDados(payload: Partial<VoluntarioMeusDados> & { novaSenha?: string }): Observable<{ mensagem: string }> {
    return this.http.put<{ mensagem: string }>(`${this.apiUrl}/meus-dados`, payload);
  }

  listarCultosPlanejados(ano: number, mes: number): Observable<VoluntarioCultosPlanejadosResponse> {
    return this.http.get<VoluntarioCultosPlanejadosResponse>(`${this.apiUrl}/cultos-planejados?ano=${ano}&mes=${mes}`);
  }

  obterMinhaDisponibilidadePorCulto(cultoId: number): Observable<VoluntarioDisponibilidadeCultoDetalhe> {
    return this.http.get<VoluntarioDisponibilidadeCultoDetalhe>(`${this.apiUrl}/culto/${cultoId}/disponibilidade`);
  }

  informarDisponibilidade(cultoId: number, payload: { disponivel: boolean; ministerioIds: number[]; observacao?: string | null }): Observable<{ mensagem: string }> {
    return this.http.put<{ mensagem: string }>(`${this.apiUrl}/culto/${cultoId}/disponibilidade`, payload);
  }

  listarDisponibilidadesGestao(params: {
    cultoId?: number | null;
    ministerioId?: number | null;
    statusDisponibilidadeId?: number | null;
  }): Observable<DisponibilidadeGestaoItem[]> {
    const query = [
      `cultoId=${params.cultoId || ''}`,
      `ministerioId=${params.ministerioId || ''}`,
      `statusDisponibilidadeId=${params.statusDisponibilidadeId || ''}`
    ].join('&');

    return this.http.get<DisponibilidadeGestaoItem[]>(`${environment.apiUrl}/disponibilidades?${query}`);
  }

  listarStatusDisponibilidade(): Observable<StatusDisponibilidadeVoluntario[]> {
    return this.http.get<StatusDisponibilidadeVoluntario[]>(`${environment.apiUrl}/disponibilidades/status`);
  }

  obterStatusGoogleCalendar(): Observable<VoluntarioGoogleCalendarStatus> {
    return this.http.get<VoluntarioGoogleCalendarStatus>(`${this.apiUrl}/google-calendar/status`);
  }

  iniciarConexaoGoogleCalendar(): Observable<{ authUrl: string }> {
    return this.http.post<{ authUrl: string }>(`${this.apiUrl}/google-calendar/connect`, {});
  }

  listarCalendariosGoogleCalendar(): Observable<VoluntarioGoogleCalendarItem[]> {
    return this.http.get<VoluntarioGoogleCalendarItem[]>(`${this.apiUrl}/google-calendar/calendars`);
  }

  selecionarCalendarioGoogleCalendar(calendarId: string): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/google-calendar/select-calendar`, { calendarId });
  }

  criarCalendarioGoogleCalendar(): Observable<{ mensagem: string; calendarioGoogleId: string; calendarioGoogleNome: string }> {
    return this.http.post<{ mensagem: string; calendarioGoogleId: string; calendarioGoogleNome: string }>(`${this.apiUrl}/google-calendar/create-calendar`, {});
  }

  sincronizarGoogleCalendar(): Observable<{ mensagem: string; sincronizados: number; falhas: number }> {
    return this.http.post<{ mensagem: string; sincronizados: number; falhas: number }>(`${this.apiUrl}/google-calendar/sync`, {});
  }

  desconectarGoogleCalendar(): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/google-calendar/disconnect`, {});
  }
}
