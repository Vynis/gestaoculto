import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TemplateCulto, TemplateCultoRequest } from '../models/template-culto.models';

@Injectable({ providedIn: 'root' })
export class TemplateCultoService {
  private readonly apiUrl = `${environment.apiUrl}/templates-culto`;

  constructor(private readonly http: HttpClient) {}

  listar(): Observable<TemplateCulto[]> {
    return this.http.get<TemplateCulto[]>(this.apiUrl);
  }

  criar(payload: TemplateCultoRequest): Observable<TemplateCulto> {
    return this.http.post<TemplateCulto>(this.apiUrl, payload);
  }

  atualizar(id: number, payload: TemplateCultoRequest): Observable<TemplateCulto> {
    return this.http.put<TemplateCulto>(`${this.apiUrl}/${id}`, payload);
  }

  excluir(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/${id}`);
  }
}
