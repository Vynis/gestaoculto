import { Injectable } from '@angular/core';
import { CanDeactivate } from '@angular/router';
import { confirmarAcao } from '../utils/confirm-dialog.util';
import { RepertorioComponent } from '../../modules/repertorio/repertorio.component';

@Injectable({ providedIn: 'root' })
export class RepertorioCanDeactivateGuard implements CanDeactivate<RepertorioComponent> {
  async canDeactivate(component: RepertorioComponent): Promise<boolean> {
    if (!component.temAlteracoesNaoSalvas()) {
      return true;
    }

    return confirmarAcao(
      'Alterações não salvas',
      'Existem alterações no repertório que ainda não foram salvas. Deseja sair mesmo assim?',
      'warning',
      'Sair sem salvar',
      '#c62828'
    );
  }
}
