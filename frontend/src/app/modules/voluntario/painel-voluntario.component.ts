import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { forkJoin } from 'rxjs';
import {
  VoluntarioCultoPlanejado,
  VoluntarioDisponibilidadeCultoDetalhe,
  VoluntarioEscalaDetalheResponse,
  VoluntarioPainelResponse
} from '../../core/models/portal-voluntario.models';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

@Component({
  selector: 'app-painel-voluntario',
  templateUrl: './painel-voluntario.component.html',
  styleUrls: ['./painel-voluntario.component.scss']
})
export class PainelVoluntarioComponent implements OnInit {
  dados: VoluntarioPainelResponse | null = null;
  proximosCompromissosModal: any[] = [];
  pendentesConfirmacaoModal: any[] = [];
  cultosNaoRespondidosModal: VoluntarioCultoPlanejado[] = [];
  totalNaoRespondidos = 0;
  detalheEscala: VoluntarioEscalaDetalheResponse | null = null;
  detalheDisponibilidade: VoluntarioDisponibilidadeCultoDetalhe | null = null;

  modalProximosAberto = false;
  modalPendentesAberto = false;
  modalNaoRespondidosAberto = false;
  modalDetalheAberto = false;
  modalDisponibilidadeAberto = false;

  readonly disponibilidadeForm = this.fb.group({
    disponivel: [true],
    ministerioIds: [[] as number[]],
    observacao: ['']
  });

  constructor(
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly toastr: NbToastrService,
    private readonly fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.portalVoluntarioService.obterPainel().subscribe((res) => {
      this.dados = res;
    });
    this.carregarCultosNaoRespondidos();
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

  abrirModalProximosCompromissos(): void {
    this.carregarCompromissosParaDashboard(() => {
      this.modalProximosAberto = true;
    });
  }

  abrirModalPendentesConfirmacao(): void {
    this.carregarCompromissosParaDashboard(() => {
      this.modalPendentesAberto = true;
    });
  }

  fecharModalProximos(): void {
    this.modalProximosAberto = false;
  }

  fecharModalPendentes(): void {
    this.modalPendentesAberto = false;
  }

  abrirModalNaoRespondidos(): void {
    this.carregarCultosNaoRespondidos(() => {
      this.modalNaoRespondidosAberto = true;
    });
  }

  fecharModalNaoRespondidos(): void {
    this.modalNaoRespondidosAberto = false;
  }

  fecharModalDetalhe(): void {
    this.modalDetalheAberto = false;
    this.detalheEscala = null;
  }

  verDetalheEscala(id: number): void {
    this.portalVoluntarioService.obterDetalheEscala(id).subscribe((res) => {
      this.detalheEscala = res;
      this.modalDetalheAberto = true;
    });
  }

  confirmarPresenca(item: any): void {
    this.portalVoluntarioService.confirmarPresenca(item.id).subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem, 'Escala');
        this.recarregarPainel();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível confirmar.', 'Atenção');
      }
    });
  }

  abrirDisponibilidade(cultoId: number): void {
    this.portalVoluntarioService.obterMinhaDisponibilidadePorCulto(cultoId).subscribe((res) => {
      this.detalheDisponibilidade = res;
      this.modalDisponibilidadeAberto = true;
      this.disponibilidadeForm.patchValue({
        disponivel: res.disponibilidade?.statusCodigo !== 'INDISPONIVEL',
        ministerioIds: res.disponibilidade?.ministerioIds || [],
        observacao: res.disponibilidade?.observacao || ''
      });
    });
  }

  fecharModalDisponibilidade(): void {
    this.modalDisponibilidadeAberto = false;
    this.detalheDisponibilidade = null;
  }

  alternarMinisterio(ministerioId: number, marcado: boolean): void {
    const atuais = this.disponibilidadeForm.controls.ministerioIds.value || [];
    const atualizados = marcado
      ? Array.from(new Set([...atuais, ministerioId]))
      : atuais.filter((id) => id !== ministerioId);
    this.disponibilidadeForm.patchValue({ ministerioIds: atualizados });
  }

  salvarDisponibilidade(): void {
    if (!this.detalheDisponibilidade) {
      return;
    }

    const raw = this.disponibilidadeForm.getRawValue();
    this.portalVoluntarioService
      .informarDisponibilidade(this.detalheDisponibilidade.culto.id, {
        disponivel: !!raw.disponivel,
        ministerioIds: raw.ministerioIds || [],
        observacao: raw.observacao || null
      })
      .subscribe({
        next: (res) => {
          this.toastr.success(res.mensagem, 'Disponibilidade');
          this.fecharModalDisponibilidade();
          this.recarregarPainel();
        },
        error: (error) => {
          this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar disponibilidade.', 'Atenção');
        }
      });
  }

  private recarregarPainel(): void {
    this.ngOnInit();
    this.carregarCompromissosParaDashboard();
  }

  private carregarCultosNaoRespondidos(callback?: () => void): void {
    const hoje = new Date();
    const baseAno = hoje.getFullYear();
    const baseMes = hoje.getMonth() + 1;

    const meses = [0, 1, 2].map((offset) => {
      const data = new Date(baseAno, baseMes - 1 + offset, 1);
      return { ano: data.getFullYear(), mes: data.getMonth() + 1 };
    });

    forkJoin(meses.map((item) => this.portalVoluntarioService.listarCultosPlanejados(item.ano, item.mes))).subscribe((respostas) => {
      const mapa = new Map<number, VoluntarioCultoPlanejado>();
      respostas.forEach((resposta) => {
        (resposta.cultos || []).forEach((culto) => {
          if (!mapa.has(culto.id)) {
            mapa.set(culto.id, culto);
          }
        });
      });

      const lista = Array.from(mapa.values())
        .filter((culto) => culto.podeResponder && culto.statusDisponibilidadeCodigo === 'NAO_RESPONDIDO')
        .sort((a, b) => {
          const da = new Date(a.dataCulto).getTime();
          const db = new Date(b.dataCulto).getTime();
          return da - db;
        });

      this.cultosNaoRespondidosModal = lista;
      this.totalNaoRespondidos = lista.length;

      if (callback) {
        callback();
      }
    });
  }

  private carregarCompromissosParaDashboard(callback?: () => void): void {
    this.portalVoluntarioService.listarMinhaEscala().subscribe((res) => {
      const hoje = new Date();
      hoje.setHours(0, 0, 0, 0);

      const futuros = (res || []).filter((item) => {
        const data = new Date(item.dataCulto);
        data.setHours(0, 0, 0, 0);
        return data >= hoje;
      });

      this.proximosCompromissosModal = futuros;
      this.pendentesConfirmacaoModal = futuros.filter((item) => this.isPendenteConfirmacao(item));

      if (callback) {
        callback();
      }
    });
  }

  isPendenteConfirmacao(item: any): boolean {
    const statusId = Number(item?.presencaStatusId ?? 0);
    const statusNome = String(item?.presencaStatusNome || '').trim().toLowerCase();

    if (statusNome.includes('confirm')) {
      return false;
    }

    return statusId === 1 || statusNome.includes('pendente');
  }
}
