import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
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
    return this.http.get<unknown>(`${this.apiUrl}/voluntarios`).pipe(
      map((response) => {
        return this.extrairListaVoluntarios(response);
      })
    );
  }

  private extrairListaVoluntarios(response: unknown): Voluntario[] {
    if (Array.isArray(response)) {
      return response as Voluntario[];
    }

    if (!response || typeof response !== 'object') {
      return [];
    }

    const container = response as {
      voluntarios?: Voluntario[];
      itens?: Voluntario[];
      items?: Voluntario[];
      data?: Voluntario[];
      dados?: Voluntario[];
      value?: Voluntario[];
      result?: Voluntario[];
      results?: Voluntario[];
      payload?: Voluntario[];
      $values?: Voluntario[];
    };

    const listas = [
      container.voluntarios,
      container.itens,
      container.items,
      container.data,
      container.dados,
      container.value,
      container.result,
      container.results,
      container.payload,
      container.$values
    ];

    const primeiraListaValida = listas.find((lista) => Array.isArray(lista));
    if (primeiraListaValida) {
      return primeiraListaValida;
    }

    const valores = Object.values(container);
    const possiveisVoluntarios = valores.filter((valor) => !!valor && typeof valor === 'object' && !Array.isArray(valor)) as unknown as Array<Record<string, unknown>>;
    if (possiveisVoluntarios.length && possiveisVoluntarios.every((voluntario) => 'id' in voluntario && 'nome' in voluntario)) {
      return possiveisVoluntarios as unknown as Voluntario[];
    }

    return [];
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
