import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Ministerio, UsuarioOpcaoVoluntario, Voluntario } from '../models/cadastro.models';

@Injectable({ providedIn: 'root' })
export class CadastroService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  listarMinisterios(): Observable<Ministerio[]> {
    return this.http.get<Ministerio[]>(`${this.apiUrl}/ministerios`);
  }

  listarVoluntarios(): Observable<Voluntario[]> {
    return this.http.get<Voluntario[]>(`${this.apiUrl}/voluntarios`);
  }

  listarUsuariosParaVoluntario(): Observable<{ usuarios: UsuarioOpcaoVoluntario[] }> {
    return this.http.get<{ usuarios: UsuarioOpcaoVoluntario[] }>(`${this.apiUrl}/voluntarios/opcoes`);
  }

  criarVoluntario(dto: Voluntario): Observable<Voluntario> {
    return this.http.post<Voluntario>(`${this.apiUrl}/voluntarios`, dto);
  }

  atualizarVoluntario(id: number, dto: Voluntario): Observable<Voluntario> {
    return this.http.put<Voluntario>(`${this.apiUrl}/voluntarios/${id}`, dto);
  }

  excluirVoluntario(id: number): Observable<{ mensagem: string }> {
    return this.http.delete<{ mensagem: string }>(`${this.apiUrl}/voluntarios/${id}`);
  }
}
