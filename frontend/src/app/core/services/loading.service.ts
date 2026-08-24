import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private requisicoesAtivas = 0;
  private readonly loadingSubject = new BehaviorSubject<boolean>(false);

  readonly loading$: Observable<boolean> = this.loadingSubject.asObservable();

  iniciar(): void {
    this.requisicoesAtivas += 1;
    if (this.requisicoesAtivas === 1) {
      this.loadingSubject.next(true);
    }
  }

  finalizar(): void {
    this.requisicoesAtivas = Math.max(0, this.requisicoesAtivas - 1);
    if (this.requisicoesAtivas === 0) {
      this.loadingSubject.next(false);
    }
  }
}
