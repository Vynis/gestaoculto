import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { map, Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { PortalVoluntarioService } from '../services/portal-voluntario.service';

@Injectable({ providedIn: 'root' })
export class LouvorGuard implements CanActivate {
  constructor(
    private readonly authService: AuthService,
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly router: Router
  ) {}

  canActivate(): Observable<boolean | UrlTree> | boolean | UrlTree {
    if (!this.authService.possuiAlgumPerfil(['VOLUNTARIO'])) {
      return this.router.parseUrl('/dashboard');
    }

    return this.portalVoluntarioService.listarMeusMinisterios().pipe(
      map((ministerios) => ministerios.some((item) => item.ministerioCodigo === 'LOUVOR')
        ? true
        : this.router.parseUrl('/voluntario/painel')),
      catchError(() => of(this.router.parseUrl('/voluntario/painel')))
    );
  }
}
