import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map, shareReplay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { BuildVersionInfo, VersionDisplayInfo } from '../models/version.models';

@Injectable({ providedIn: 'root' })
export class VersionService {
  private cache$: Observable<VersionDisplayInfo> | null = null;

  constructor(private readonly http: HttpClient) {}

  obterVersoes(): Observable<VersionDisplayInfo> {
    if (!this.cache$) {
      this.cache$ = forkJoin({
        frontend: this.http.get<BuildVersionInfo>('assets/version.json').pipe(
          catchError(() => of(this.fallbackFrontend()))
        ),
        backend: this.http.get<BuildVersionInfo>(`${environment.apiUrl}/version`).pipe(
          catchError(() => of(null))
        )
      }).pipe(
        map((payload) => ({
          frontend: payload.frontend,
          backend: payload.backend
        })),
        shareReplay(1)
      );
    }

    return this.cache$;
  }

  private fallbackFrontend(): BuildVersionInfo {
    return {
      name: 'gestaoculto-web',
      version: '0.0.0',
      commit: 'unknown',
      buildDate: '',
      environment: environment.production ? 'production' : 'development'
    };
  }
}
