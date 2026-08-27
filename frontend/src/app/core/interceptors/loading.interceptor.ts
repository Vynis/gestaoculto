import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable, finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  static readonly SKIP_HEADER = 'X-Skip-Loading';

  constructor(private readonly loadingService: LoadingService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const deveIgnorar = req.headers.has(LoadingInterceptor.SKIP_HEADER);
    const request = deveIgnorar ? req.clone({ headers: req.headers.delete(LoadingInterceptor.SKIP_HEADER) }) : req;

    if (deveIgnorar) {
      return next.handle(request);
    }

    this.loadingService.iniciar();
    return next.handle(request).pipe(finalize(() => this.loadingService.finalizar()));
  }
}
