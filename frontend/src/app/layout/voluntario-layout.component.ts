import { Component } from '@angular/core';
import { NbMenuItem } from '@nebular/theme';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-voluntario-layout',
  templateUrl: './voluntario-layout.component.html',
  styleUrls: ['./voluntario-layout.component.scss']
})
export class VoluntarioLayoutComponent {
  readonly menu: NbMenuItem[] = [
    { title: 'Painel', icon: 'home-outline', link: '/voluntario/painel', home: true },
    { title: 'Calendário', icon: 'calendar-outline', link: '/voluntario/calendario' },
    { title: 'Minha escala', icon: 'clock-outline', link: '/voluntario/minha-escala' },
    { title: 'Meus ministérios', icon: 'layers-outline', link: '/voluntario/meus-ministerios' },
    { title: 'Equipe do ministério', icon: 'people-outline', link: '/voluntario/colegas' },
    { title: 'Meus dados', icon: 'person-outline', link: '/voluntario/meus-dados' }
  ];

  constructor(public readonly authService: AuthService) {}

  sair(): void {
    this.authService.logout();
  }
}
