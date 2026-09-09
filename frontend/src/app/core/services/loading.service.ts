import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private requisicoesAtivas = 0;
  private atualizacaoAgendada = false;
  private readonly loadingSubject = new BehaviorSubject<boolean>(false);

  readonly loading$: Observable<boolean> = this.loadingSubject.asObservable();

  iniciar(): void {
    this.requisicoesAtivas += 1;
    this.agendarAtualizacao();
  }

  finalizar(): void {
    this.requisicoesAtivas = Math.max(0, this.requisicoesAtivas - 1);
    this.agendarAtualizacao();
  }

  private agendarAtualizacao(): void {
    if (this.atualizacaoAgendada) {
      return;
    }

    this.atualizacaoAgendada = true;
    setTimeout(() => {
      this.atualizacaoAgendada = false;
      this.loadingSubject.next(this.requisicoesAtivas > 0);
    });
  }
}
