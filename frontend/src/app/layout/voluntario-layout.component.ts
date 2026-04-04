import { Component, OnInit } from '@angular/core';
import { NbMenuItem } from '@nebular/theme';
import { AuthService } from '../core/services/auth.service';
import { VersionDisplayInfo } from '../core/models/version.models';
import { VersionService } from '../core/services/version.service';

@Component({
  selector: 'app-voluntario-layout',
  templateUrl: './voluntario-layout.component.html',
  styleUrls: ['./voluntario-layout.component.scss']
})
export class VoluntarioLayoutComponent implements OnInit {
  readonly menu: NbMenuItem[] = [
    { title: 'Painel', icon: 'home-outline', link: '/voluntario/painel', home: true },
    { title: 'Calendário', icon: 'calendar-outline', link: '/voluntario/calendario' },
    { title: 'Minha escala', icon: 'clock-outline', link: '/voluntario/minha-escala' },
    { title: 'Meus ministérios', icon: 'layers-outline', link: '/voluntario/meus-ministerios' },
    { title: 'Equipe do ministério', icon: 'people-outline', link: '/voluntario/colegas' },
    { title: 'Meus dados', icon: 'person-outline', link: '/voluntario/meus-dados' }
  ];
  versoes: VersionDisplayInfo | null = null;

  constructor(
    public readonly authService: AuthService,
    private readonly versionService: VersionService
  ) {}

  ngOnInit(): void {
    this.versionService.obterVersoes().subscribe((data) => {
      this.versoes = data;
    });
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
