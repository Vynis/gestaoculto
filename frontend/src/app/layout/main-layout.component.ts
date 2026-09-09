import { Component, OnInit } from '@angular/core';
import { NbMenuItem, NbSidebarService } from '@nebular/theme';
import { AuthService } from '../core/services/auth.service';
import { VersionDisplayInfo } from '../core/models/version.models';
import { VersionService } from '../core/services/version.service';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss']
})
export class MainLayoutComponent implements OnInit {
  readonly menu: NbMenuItem[];
  versoes: VersionDisplayInfo | null = null;

  constructor(
    public readonly authService: AuthService,
    private readonly sidebarService: NbSidebarService,
    private readonly versionService: VersionService
  ) {
    this.menu = this.montarMenu();
  }

  ngOnInit(): void {
    this.versionService.obterVersoes().subscribe((data) => {
      this.versoes = data;
    });
  }

  private montarMenu(): NbMenuItem[] {
    const podeGerenciarPlanejamento = this.authService.possuiPerfil(['ADMIN', 'GESTAO_CULTO']);
    const podeGerenciarMinisterios = podeGerenciarPlanejamento;
    const itens: NbMenuItem[] = [
      { title: 'Dashboard', icon: 'grid-outline', link: '/dashboard', home: true },
      {
        title: 'Planejamento',
        icon: 'calendar-outline',
        expanded: true,
        children: [
          ...(podeGerenciarPlanejamento ? [
            { title: 'Cultos', link: '/cultos' },
            { title: 'Recorrências', link: '/recorrencias-culto' },
            { title: 'Templates', link: '/templates' },
            { title: 'Cronograma', link: '/cronograma' }
          ] : []),
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
          ...(podeGerenciarMinisterios ? [{ title: 'Ministérios', link: '/ministerios' }] : []),
          { title: 'Voluntários', link: '/voluntarios' },
          { title: 'Disponibilidades', link: '/disponibilidades' },
          { title: 'Convidados', link: '/convidados' }
        ]
      },
      {
        title: 'Relatórios',
        icon: 'file-text-outline',
        children: [
          { title: 'Relatório culto', link: '/relatorio-culto' },
          { title: 'Escala mensal', link: '/relatorio-escala-mensal' }
        ]
      }
    ];

    itens.push({ title: 'Wiki / Ajuda', icon: 'book-open-outline', link: '/wiki', target: '_blank' });

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

  get versaoFrontendLabel(): string {
    if (!this.versoes?.frontend) {
      return '';
    }

    return `${this.versoes.frontend.version} (${this.versoes.frontend.commit})`;
  }

  get versaoBackendLabel(): string {
    if (!this.versoes?.backend) {
      return '';
    }

    return `${this.versoes.backend.version} (${this.versoes.backend.commit})`;
  }
}
