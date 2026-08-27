import { Component, OnInit } from '@angular/core';
import { VoluntarioColegaMinisterio } from '../../core/models/portal-voluntario.models';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

interface EquipeMinisterio {
  ministerioId: number;
  ministerioNome: string;
  colegas: VoluntarioColegaMinisterio[];
}

@Component({
  selector: 'app-colegas-ministerio-voluntario',
  templateUrl: './colegas-ministerio-voluntario.component.html',
  styleUrls: ['./colegas-ministerio-voluntario.component.scss']
})
export class ColegasMinisterioVoluntarioComponent implements OnInit {
  itens: VoluntarioColegaMinisterio[] = [];
  equipes: EquipeMinisterio[] = [];

  constructor(private readonly portalVoluntarioService: PortalVoluntarioService) {}

  ngOnInit(): void {
    this.portalVoluntarioService.listarColegasMinisterio().subscribe((res) => {
      this.itens = res;
      this.equipes = this.organizarPorEquipe(res);
    });
  }

  private organizarPorEquipe(itens: VoluntarioColegaMinisterio[]): EquipeMinisterio[] {
    const grupos = new Map<number, EquipeMinisterio>();

    for (const item of itens) {
      const grupoExistente = grupos.get(item.ministerioId);

      if (grupoExistente) {
        grupoExistente.colegas.push(item);
        continue;
      }

      grupos.set(item.ministerioId, {
        ministerioId: item.ministerioId,
        ministerioNome: item.ministerioNome,
        colegas: [item]
      });
    }

    return Array.from(grupos.values())
      .map((grupo) => ({
        ...grupo,
        colegas: [...grupo.colegas].sort((a, b) => a.voluntarioNome.localeCompare(b.voluntarioNome))
      }))
      .sort((a, b) => a.ministerioNome.localeCompare(b.ministerioNome));
  }
}
