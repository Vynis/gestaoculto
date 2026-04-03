import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Culto, CultoRequest } from '../models/culto.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CultoService {
  private readonly apiUrl = `${environment.apiUrl}/cultos`;

  constructor(private readonly http: HttpClient) {}

  listar(): Observable<Culto[]> {
    return this.http.get<Culto[]>(this.apiUrl);
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
}
