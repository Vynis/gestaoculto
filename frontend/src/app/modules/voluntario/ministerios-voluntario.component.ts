import { Component, OnInit } from '@angular/core';
import { VoluntarioMinisterioResumo } from '../../core/models/portal-voluntario.models';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

@Component({
  selector: 'app-ministerios-voluntario',
  templateUrl: './ministerios-voluntario.component.html'
})
export class MinisteriosVoluntarioComponent implements OnInit {
  itens: VoluntarioMinisterioResumo[] = [];

  constructor(private readonly portalVoluntarioService: PortalVoluntarioService) {}

  ngOnInit(): void {
    this.portalVoluntarioService.listarMeusMinisterios().subscribe((res) => {
      this.itens = res;
    });
  }
}
