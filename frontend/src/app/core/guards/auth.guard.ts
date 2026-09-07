import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree {
    if (this.authService.estaAutenticado()) {
      if (this.authService.deveTrocarSenha() && state.url !== '/auth/trocar-senha') {
        return this.router.parseUrl('/auth/trocar-senha');
      }
      if (!this.authService.deveTrocarSenha() && state.url === '/auth/trocar-senha') {
        return this.router.parseUrl(this.authService.destinoPadraoPosLogin());
      }
      return true;
    }

    const destino = state.url.startsWith('/voluntario')
      ? '/voluntario/login'
      : '/auth/login';

    return this.router.parseUrl(`${destino}?returnUrl=${encodeURIComponent(state.url)}`);
  }
}
