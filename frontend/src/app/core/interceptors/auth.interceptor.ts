import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { catchError, Observable, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly authService: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authService.getToken();
    if (!token) {
      return next.handle(req);
    }

    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });

    return next.handle(cloned).pipe(
      catchError((error) => {
        if (error?.status === 403 && error?.error?.codigo === 'TROCA_SENHA_OBRIGATORIA') {
          this.authService.exigirTrocaSenha();
        }
        return throwError(() => error);
      })
    );
  }
}
