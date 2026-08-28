import { Component, OnInit } from '@angular/core';
import { NbMenuItem, NbSidebarService } from '@nebular/theme';
import { AuthService } from '../core/services/auth.service';
import { VersionDisplayInfo } from '../core/models/version.models';
import { VersionService } from '../core/services/version.service';
import { PortalVoluntarioService } from '../core/services/portal-voluntario.service';

@Component({
  selector: 'app-voluntario-layout',
  templateUrl: './voluntario-layout.component.html',
  styleUrls: ['./voluntario-layout.component.scss']
})
export class VoluntarioLayoutComponent implements OnInit {
  private readonly menuBase: NbMenuItem[] = [
    { title: 'Painel', icon: 'home-outline', link: '/voluntario/painel', home: true },
    { title: 'Calendário', icon: 'calendar-outline', link: '/voluntario/calendario' },
    { title: 'Minha escala', icon: 'clock-outline', link: '/voluntario/minha-escala' },
    { title: 'Meus ministérios', icon: 'layers-outline', link: '/voluntario/meus-ministerios' },
    { title: 'Equipe do ministério', icon: 'people-outline', link: '/voluntario/colegas' },
    { title: 'Meus dados', icon: 'person-outline', link: '/voluntario/meus-dados' }
  ];
  menu: NbMenuItem[] = [...this.menuBase];
  versoes: VersionDisplayInfo | null = null;

  constructor(
    public readonly authService: AuthService,
    private readonly sidebarService: NbSidebarService,
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly versionService: VersionService
  ) {}

  ngOnInit(): void {
    this.portalVoluntarioService.listarMeusMinisterios().subscribe((ministerios) => {
      if (ministerios.some((item) => item.ministerioCodigo === 'LOUVOR')) {
        this.menu = [
          ...this.menuBase.slice(0, 3),
          { title: 'Repertório', icon: 'music-outline', link: '/voluntario/repertorio' },
          ...this.menuBase.slice(3)
        ];
      } else {
        this.menu = [...this.menuBase];
      }
    });

    this.versionService.obterVersoes().subscribe((data) => {
      this.versoes = data;
    });
  }

  sair(): void {
    this.authService.logout();
  }

  alternarMenu(): void {
    this.sidebarService.toggle(true, 'voluntario-menu-sidebar');
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
