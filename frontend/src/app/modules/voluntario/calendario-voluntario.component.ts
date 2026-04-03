import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import {
  VoluntarioCalendarioResponse,
  VoluntarioCultoPlanejado,
  VoluntarioDisponibilidadeCultoDetalhe
} from '../../core/models/portal-voluntario.models';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

interface DiaCalendario {
  data: Date;
  chave: string;
  total: number;
  foraMes: boolean;
}

@Component({
  selector: 'app-calendario-voluntario',
  templateUrl: './calendario-voluntario.component.html',
  styleUrls: ['./calendario-voluntario.component.scss']
})
export class CalendarioVoluntarioComponent implements OnInit {
  ano = new Date().getFullYear();
  mes = new Date().getMonth() + 1;
  resposta: VoluntarioCalendarioResponse | null = null;
  dias: DiaCalendario[] = [];
  diaSelecionado = '';
  cultosPlanejados: VoluntarioCultoPlanejado[] = [];
  modalDisponibilidadeAberto = false;
  detalheDisponibilidade: VoluntarioDisponibilidadeCultoDetalhe | null = null;

  readonly disponibilidadeForm = this.fb.group({
    disponivel: [true],
    ministerioIds: [[] as number[]],
    observacao: ['']
  });

  constructor(
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly fb: FormBuilder,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.portalVoluntarioService.obterCalendario(this.ano, this.mes).subscribe((res) => {
      this.resposta = res;
      this.dias = this.montarGrade(res);
      this.diaSelecionado = res.dias[0]?.data || '';
    });

    this.portalVoluntarioService.listarCultosPlanejados(this.ano, this.mes).subscribe((res) => {
      this.cultosPlanejados = res.cultos || [];
    });
  }

  mesAnterior(): void {
    if (this.mes === 1) {
      this.mes = 12;
      this.ano -= 1;
    } else {
      this.mes -= 1;
    }
    this.carregar();
  }

  mesProximo(): void {
    if (this.mes === 12) {
      this.mes = 1;
      this.ano += 1;
    } else {
      this.mes += 1;
    }
    this.carregar();
  }

  selecionarDia(chave: string): void {
    this.diaSelecionado = chave;
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
          this.carregar();
        },
        error: (error) => {
          this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar disponibilidade.', 'Atenção');
        }
      });
  }

  statusClass(codigo: string): string {
    switch (codigo) {
      case 'DISPONIVEL':
        return 'status ok';
      case 'INDISPONIVEL':
        return 'status no';
      case 'ESCALADO':
        return 'status scale';
      default:
        return 'status neutral';
    }
  }

  nomesMinisterios(culto: VoluntarioCultoPlanejado): string {
    return (culto.ministerios || []).map((item) => item.ministerioNome).join(', ');
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

  ministeriosDoDia(chave: string): string[] {
    const itens = (this.resposta?.dias || []).find((x) => x.data.startsWith(chave))?.itens || [];
    const nomes = itens
      .map((item) => (item.ministerioNome || '').trim())
      .filter((nome) => !!nome);

    return Array.from(new Set(nomes));
  }

  resumoMinisteriosDoDia(chave: string): string[] {
    return this.ministeriosDoDia(chave).slice(0, 2);
  }

  possuiMaisMinisteriosNoDia(chave: string): boolean {
    return this.ministeriosDoDia(chave).length > 2;
  }

  totalMinisteriosDia(chave: string): number {
    return this.ministeriosDoDia(chave).length;
  }

  get eventosDiaSelecionado() {
    return (this.resposta?.dias || []).find((x) => x.data.startsWith(this.diaSelecionado))?.itens || [];
  }

  private montarGrade(res: VoluntarioCalendarioResponse): DiaCalendario[] {
    const base = new Date(res.ano, res.mes - 1, 1);
    const inicioSemana = new Date(base);
    inicioSemana.setDate(base.getDate() - base.getDay());

    const mapaTotais = new Map((res.dias || []).map((d) => [d.data.slice(0, 10), d.total]));
    const lista: DiaCalendario[] = [];
    for (let i = 0; i < 42; i += 1) {
      const dia = new Date(inicioSemana);
      dia.setDate(inicioSemana.getDate() + i);
      const chave = this.chaveData(dia);
      lista.push({
        data: dia,
        chave,
        total: mapaTotais.get(chave) || 0,
        foraMes: dia.getMonth() + 1 !== res.mes
      });
    }

    return lista;
  }

  private chaveData(data: Date): string {
    const ano = data.getFullYear();
    const mes = `${data.getMonth() + 1}`.padStart(2, '0');
    const dia = `${data.getDate()}`.padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
  }
}
