import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Convidado } from '../models/convidado.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ConvidadoService {
  private readonly apiUrl = `${environment.apiUrl}/convidados`;

  constructor(private readonly http: HttpClient) {}

  listar(cultoId?: number): Observable<Convidado[]> {
    const params = cultoId ? new HttpParams().set('cultoId', cultoId.toString()) : undefined;
    return this.http.get<Convidado[]>(this.apiUrl, { params });
  }

  criar(payload: Partial<Convidado>): Observable<Convidado> {
    return this.http.post<Convidado>(this.apiUrl, payload);
  }

  atualizar(id: number, payload: Partial<Convidado>): Observable<Convidado> {
    return this.http.put<Convidado>(`${this.apiUrl}/${id}`, payload);
  }
}
