import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { AuthResponse, AuthSession } from '../models/auth.models';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
          }) => void;
          prompt: () => void;
        };
      };
    };
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly storageKey = 'gc.auth';
  private readonly session$ = new BehaviorSubject<AuthSession | null>(this.loadSession());

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  login(email: string, senha: string, manterConectado: boolean): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, senha, manterConectado }).pipe(
      tap((response) => this.storeSession(response))
    );
  }

  aplicarSessao(response: AuthResponse): void {
    this.storeSession(response);
  }

  loginComGoogle(manterConectado: boolean): Promise<AuthResponse> {
    return new Promise((resolve, reject) => {
      if (!window.google) {
        reject(new Error('Google não carregado. Verifique o ClientId.'));
        return;
      }

      window.google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback: (response: { credential: string }) => {
          this.http
            .post<AuthResponse>(`${this.apiUrl}/google`, {
              idToken: response.credential,
              manterConectado
            })
            .pipe(tap((auth) => this.storeSession(auth)))
            .subscribe({ next: resolve, error: reject });
        }
      });

      window.google.accounts.id.prompt();
    });
  }

  getToken(): string | null {
    return this.session$.value?.token ?? null;
  }

  getUsuarioAtual(): AuthSession | null {
    return this.session$.value;
  }

  possuiPerfil(perfis: string[]): boolean {
    const atual = this.session$.value;
    if (!atual) {
      return false;
    }

    return perfis.some((p) => atual.perfis.includes(p));
  }

  possuiAlgumPerfil(perfis: string[]): boolean {
    return this.possuiPerfil(perfis);
  }

  ehVoluntario(): boolean {
    return this.possuiPerfil(['VOLUNTARIO']);
  }

  destinoPadraoPosLogin(): string {
    if (this.ehVoluntario()) {
      return '/voluntario/painel';
    }

    return '/dashboard';
  }

  estaAutenticado(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    const destino = this.ehVoluntario() ? '/voluntario/login' : '/auth/login';
    localStorage.removeItem(this.storageKey);
    this.session$.next(null);
    this.router.navigate([destino]);
  }

  private storeSession(response: AuthResponse): void {
    const session: AuthSession = {
      token: response.token,
      nome: response.nome,
      email: response.email,
      perfis: response.perfis ?? []
    };

    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.session$.next(session);
  }

  private loadSession(): AuthSession | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthSession;
    } catch {
      return null;
    }
  }
}
