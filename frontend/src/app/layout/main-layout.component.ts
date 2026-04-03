import { Component } from '@angular/core';
import { NbMenuItem, NbSidebarService } from '@nebular/theme';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss']
})
export class MainLayoutComponent {
  readonly menu: NbMenuItem[];

  constructor(
    public readonly authService: AuthService,
    private readonly sidebarService: NbSidebarService
  ) {
    this.menu = this.montarMenu();
  }

  private montarMenu(): NbMenuItem[] {
    const itens: NbMenuItem[] = [
      { title: 'Dashboard', icon: 'grid-outline', link: '/dashboard', home: true },
      {
        title: 'Planejamento',
        icon: 'calendar-outline',
        expanded: true,
        children: [
          { title: 'Cultos', link: '/cultos' },
          { title: 'Templates', link: '/templates' },
          { title: 'Cronograma', link: '/cronograma' },
          { title: 'Escalas', link: '/escalas' },
          { title: 'Músicas', link: '/musicas' },
          { title: 'Repertório', link: '/repertorio' }
        ]
      },
      {
        title: 'Pessoas e Equipes',
        icon: 'people-outline',
        expanded: true,
        children: [
          { title: 'Ministérios', link: '/ministerios' },
          { title: 'Voluntários', link: '/voluntarios' },
          { title: 'Disponibilidades', link: '/disponibilidades' },
          { title: 'Convidados', link: '/convidados' }
        ]
      },
      {
        title: 'Relatórios',
        icon: 'file-text-outline',
        children: [{ title: 'Relatório culto', link: '/relatorio-culto' }]
      }
    ];

    if (this.authService.possuiPerfil(['ADMIN', 'GESTAO_CULTO'])) {
      itens.push({
        title: 'Administração',
        icon: 'settings-2-outline',
        children: [{ title: 'Usuários', link: '/usuarios' }]
      });
    }

    return itens;
  }

  alternarMenu(): void {
    this.sidebarService.toggle(true, 'menu-sidebar');
  }

  sair(): void {
    this.authService.logout();
  }
}
