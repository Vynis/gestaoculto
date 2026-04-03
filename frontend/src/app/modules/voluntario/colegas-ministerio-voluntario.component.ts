import { Component, OnInit } from '@angular/core';
import { VoluntarioColegaMinisterio } from '../../core/models/portal-voluntario.models';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

@Component({
  selector: 'app-colegas-ministerio-voluntario',
  templateUrl: './colegas-ministerio-voluntario.component.html'
})
export class ColegasMinisterioVoluntarioComponent implements OnInit {
  itens: VoluntarioColegaMinisterio[] = [];

  constructor(private readonly portalVoluntarioService: PortalVoluntarioService) {}

  ngOnInit(): void {
    this.portalVoluntarioService.listarColegasMinisterio().subscribe((res) => {
      this.itens = res;
    });
  }
}
