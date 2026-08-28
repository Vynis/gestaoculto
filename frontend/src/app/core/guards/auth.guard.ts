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
      return true;
    }

    const destino = state.url.startsWith('/voluntario')
      ? '/voluntario/login'
      : '/auth/login';

    return this.router.parseUrl(`${destino}?returnUrl=${encodeURIComponent(state.url)}`);
  }
}
