import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { CultoService } from '../../core/services/culto.service';
import { CadastroService } from '../../core/services/cadastro.service';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';
import { Culto } from '../../core/models/culto.models';
import { Ministerio, Voluntario } from '../../core/models/cadastro.models';
import { DisponibilidadeGestaoItem, StatusDisponibilidadeVoluntario } from '../../core/models/portal-voluntario.models';
import { EscalaModalPrefill } from '../escalas/escala-modal.component';

@Component({
  selector: 'app-disponibilidades',
  templateUrl: './disponibilidades.component.html',
  styleUrls: ['./disponibilidades.component.scss']
})
export class DisponibilidadesComponent implements OnInit {
  itens: DisponibilidadeGestaoItem[] = [];
  cultos: Culto[] = [];
  voluntarios: Voluntario[] = [];
  ministerios: Ministerio[] = [];
  carregandoVoluntarios = false;
  erroVoluntarios: string | null = null;
  status: StatusDisponibilidadeVoluntario[] = [];
  modalEscalaAberto = false;
  prefillEscala: EscalaModalPrefill | null = null;
  carregando = false;
  carregouConsulta = false;

  readonly filtro = this.fb.group({
    cultoId: [null as number | null],
    ministerioId: [null as number | null],
    statusDisponibilidadeId: [null as number | null]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly cultoService: CultoService,
    private readonly cadastroService: CadastroService,
    private readonly portalVoluntarioService: PortalVoluntarioService
  ) {}

  ngOnInit(): void {
    this.cultoService.listarAtivos().subscribe((res) => {
      this.cultos = res || [];
    });

    this.cadastroService.listarMinisterios().subscribe((res) => {
      this.ministerios = res || [];
    });

    this.carregarVoluntarios();

    this.portalVoluntarioService.listarStatusDisponibilidade().subscribe((res) => {
      this.status = res || [];
    });

    this.carregar();
  }

  carregar(): void {
    const raw = this.filtro.getRawValue();
    this.carregando = true;
    this.portalVoluntarioService
      .listarDisponibilidadesGestao({
        cultoId: raw.cultoId,
        ministerioId: raw.ministerioId,
        statusDisponibilidadeId: raw.statusDisponibilidadeId
      })
      .subscribe((res) => {
        this.itens = (res || []).slice().sort((a, b) => {
          const cultoA = (a.cultoNome || '').localeCompare(b.cultoNome || '', 'pt-BR', { sensitivity: 'base' });
          if (cultoA !== 0) {
            return cultoA;
          }

          return (a.voluntarioNome || '').localeCompare(b.voluntarioNome || '', 'pt-BR', { sensitivity: 'base' });
        });
        this.carregando = false;
        this.carregouConsulta = true;
      }, () => {
        this.carregando = false;
        this.carregouConsulta = true;
      });
  }

  nomesMinisterios(item: DisponibilidadeGestaoItem): string {
    return (item.ministerios || []).map((x) => x.ministerioNome).join(', ');
  }

  formatarHorario(valor: unknown): string {
    if (!valor) {
      return '';
    }

    if (typeof valor === 'string') {
      if (valor.includes(':')) {
        return valor.slice(0, 5);
      }

      return valor;
    }

    if (typeof valor === 'object') {
      const horario = valor as { hours?: number; minutes?: number };
      if (typeof horario.hours === 'number' && typeof horario.minutes === 'number') {
        return `${`${horario.hours}`.padStart(2, '0')}:${`${horario.minutes}`.padStart(2, '0')}`;
      }
    }

    return '';
  }

  lancarNaEscala(item: DisponibilidadeGestaoItem): void {
    this.carregarVoluntarios();
    const ministerioId = item.ministerios?.[0]?.ministerioId ?? this.filtro.value.ministerioId ?? null;
    this.prefillEscala = {
      cultoId: item.cultoId,
      voluntarioId: item.voluntarioId,
      ministerioId,
      funcao: '',
      observacoes: 'Lançado a partir da disponibilidade do voluntário.'
    };
    this.modalEscalaAberto = true;
  }

  fecharModalEscala(): void {
    this.modalEscalaAberto = false;
    this.prefillEscala = null;
  }

  aoSalvarEscala(): void {
    this.fecharModalEscala();
    this.carregar();
  }

  get semRegistros(): boolean {
    return this.carregouConsulta && !this.carregando && this.itens.length === 0;
  }

  carregarVoluntarios(): void {
    this.carregandoVoluntarios = true;
    this.erroVoluntarios = null;

    this.cadastroService.listarVoluntarios().subscribe({
      next: (res) => {
        this.voluntarios = (res || [])
          .slice()
          .sort((a, b) => (a.nome || '').localeCompare(b.nome || '', 'pt-BR', { sensitivity: 'base' }));
        this.erroVoluntarios = null;
      },
      error: () => {
        if (!this.voluntarios.length) {
          this.erroVoluntarios = 'Falha ao carregar voluntários.';
        }
        this.carregandoVoluntarios = false;
      },
      complete: () => {
        this.carregandoVoluntarios = false;
      }
    });
  }
}
