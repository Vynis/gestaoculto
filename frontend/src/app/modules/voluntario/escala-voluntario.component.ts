import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { VoluntarioCompromisso, VoluntarioEscalaDetalheResponse } from '../../core/models/portal-voluntario.models';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

@Component({
  selector: 'app-escala-voluntario',
  templateUrl: './escala-voluntario.component.html',
  styleUrls: ['./escala-voluntario.component.scss']
})
export class EscalaVoluntarioComponent implements OnInit {
  itens: VoluntarioCompromisso[] = [];
  detalhe: VoluntarioEscalaDetalheResponse | null = null;

  readonly filtro = this.fb.group({
    inicio: [''],
    fim: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    const raw = this.filtro.getRawValue();
    this.portalVoluntarioService.listarMinhaEscala(raw.inicio || undefined, raw.fim || undefined).subscribe((res) => {
      this.itens = res;
    });
  }

  verDetalhe(id: number): void {
    this.portalVoluntarioService.obterDetalheEscala(id).subscribe((res) => {
      this.detalhe = res;
    });
  }

  confirmar(item: VoluntarioCompromisso): void {
    this.portalVoluntarioService.confirmarPresenca(item.id).subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem, 'Escala');
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Não foi possível confirmar.', 'Atenção');
      }
    });
  }

  formatarHorario(valor: unknown): string {
    if (!valor) {
      return '';
    }

    if (typeof valor === 'string') {
      return valor.includes(':') ? valor.slice(0, 5) : valor;
    }

    if (typeof valor === 'object') {
      const horario = valor as { hours?: number; minutes?: number };
      if (typeof horario.hours === 'number' && typeof horario.minutes === 'number') {
        return `${`${horario.hours}`.padStart(2, '0')}:${`${horario.minutes}`.padStart(2, '0')}`;
      }
    }

    return '';
  }
}
