import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CellClickedEvent, ColDef } from 'ag-grid-community';
import { Culto } from '../../core/models/culto.models';
import { RepertorioItem } from '../../core/models/repertorio.models';
import { CultoService } from '../../core/services/culto.service';
import { RepertorioService } from '../../core/services/repertorio.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

interface CultoGridRow {
  nome: string;
  tipoCulto: string;
  dataCulto: string;
  horarioInicio: string;
  statusCulto: string;
  observacoesGerais: string;
  culto: Culto;
}

@Component({
  selector: 'app-cultos',
  templateUrl: './cultos.component.html',
  styleUrls: ['./cultos.component.scss']
})
export class CultosComponent implements OnInit {
  cultos: Culto[] = [];
  cultosSelecao: Culto[] = [];
  rowData: CultoGridRow[] = [];
  filtroGrid = '';
  repertorioCultoId = 0;
  repertorioCultoNome = '';
  repertorioItens: RepertorioItem[] = [];
  carregando = false;
  modalAberto = false;
  mensagemSucesso = '';
  mensagemErro = '';
  cultoEditandoId: number | null = null;
  readonly statusOptions = [
    { id: 1, label: 'Ativo' },
    { id: 2, label: 'Inativo' }
  ];

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    tipoCulto: ['Culto de Celebração', Validators.required],
    dataCulto: ['', Validators.required],
    horarioInicio: ['19:00', Validators.required],
    statusCultoId: [1, Validators.required],
    observacoesGerais: [''],
    repeticoesDomingo: [8, [Validators.min(1), Validators.max(52)]]
  });

  readonly defaultColDef: ColDef<CultoGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 120
  };

  readonly columnDefs: ColDef<CultoGridRow>[] = [
    { headerName: 'Nome', field: 'nome', minWidth: 180 },
    { headerName: 'Tipo', field: 'tipoCulto', minWidth: 160 },
    { headerName: 'Data', field: 'dataCulto', minWidth: 120, maxWidth: 140 },
    { headerName: 'Horário', field: 'horarioInicio', minWidth: 110, maxWidth: 130 },
    { headerName: 'Status', field: 'statusCulto', minWidth: 140 },
    { headerName: 'Observações', field: 'observacoesGerais', minWidth: 180 },
    {
      headerName: 'Ações',
      field: 'culto',
      minWidth: 150,
      maxWidth: 170,
      sortable: false,
      filter: false,
      cellRenderer: () =>
        '<div class="grid-actions"><button class="grid-btn icon view" data-action="repertorio" type="button" title="Ver repertório" aria-label="Ver repertório"><span class="icon-list"></span></button><button class="grid-btn icon edit" data-action="editar" type="button" title="Editar culto" aria-label="Editar culto"><span class="icon-pencil"></span></button><button class="grid-btn icon delete" data-action="excluir" type="button" title="Excluir culto" aria-label="Excluir culto"><span class="icon-trash"></span></button></div>'
    }
  ];

  readonly paginationPageSize = 8;

  readonly localeText = {
    noRowsToShow: 'Nenhum culto cadastrado.'
  };

  constructor(
    private readonly fb: FormBuilder,
    private readonly cultoService: CultoService,
    private readonly repertorioService: RepertorioService
  ) {}

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.cultoService.listar().subscribe((data) => {
      this.cultos = data;
      this.cultosSelecao = CultoService.ordenarPorProximidade(data);
      this.rowData = this.mapearParaGrid(data);
      if (!this.repertorioCultoId && this.cultosSelecao.length > 0) {
        this.repertorioCultoId = this.cultosSelecao[0].id;
        this.carregarRepertorio();
      }
    });
  }

  carregarRepertorio(): void {
    if (!this.repertorioCultoId) {
      this.repertorioCultoNome = '';
      this.repertorioItens = [];
      return;
    }

    this.repertorioCultoNome = this.cultos.find((item) => item.id === this.repertorioCultoId)?.nome || '';

    this.repertorioService.obterPorCulto(this.repertorioCultoId).subscribe((data) => {
      this.repertorioItens = (data.itens || []).slice().sort((a, b) => a.ordem - b.ordem);
    });
  }

  onGridCellClicked(event: CellClickedEvent<CultoGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data) {
      return;
    }

    const row = event.data;
    if (!row?.culto) {
      return;
    }

    if (action === 'repertorio') {
      this.visualizarRepertorio(row.culto);
      return;
    }

    if (action === 'editar') {
      this.editar(row.culto);
      return;
    }

    if (action === 'excluir') {
      this.excluir(row.culto);
    }
  }

  visualizarRepertorio(culto: Culto): void {
    this.repertorioCultoId = culto.id;
    this.carregarRepertorio();
  }

  abrirModalNovoCulto(): void {
    this.limparFormulario();
    this.mensagemErro = '';
    this.modalAberto = true;
  }

  fecharModal(): void {
    this.modalAberto = false;
  }

  async salvar(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.mensagemSucesso = '';
    this.mensagemErro = '';

    const raw = this.form.getRawValue();
    this.carregando = true;

    const dto = {
      nome: raw.nome!,
      tipoCulto: raw.tipoCulto!,
      dataCulto: this.normalizarData(raw.dataCulto || ''),
      horarioInicio: this.normalizarHorario(raw.horarioInicio || ''),
      horarioFimPrevisto: null,
      statusCultoId: raw.statusCultoId!,
      observacoesGerais: raw.observacoesGerais || null,
      templateCultoId: null
    };

    try {
      if (this.cultoEditandoId === null) {
        await firstValueFrom(this.cultoService.criar(dto));
        this.mensagemSucesso = 'Culto criado com sucesso.';
      } else {
        await firstValueFrom(this.cultoService.atualizar(this.cultoEditandoId, dto));
        this.mensagemSucesso = 'Culto atualizado com sucesso.';
      }

      this.limparFormulario();
      this.fecharModal();
      this.carregar();
    } catch (error) {
      this.mensagemErro = this.extrairMensagemErro(error);
    } finally {
      this.carregando = false;
    }
  }

  editar(culto: Culto): void {
    this.modalAberto = true;
    this.cultoEditandoId = culto.id;
    this.mensagemSucesso = '';
    this.mensagemErro = '';

    this.form.patchValue({
      nome: culto.nome,
      tipoCulto: culto.tipoCulto,
      dataCulto: this.paraDataInput(culto.dataCulto),
      horarioInicio: this.paraHorarioInput(culto.horarioInicio),
      statusCultoId: culto.statusCultoId,
      observacoesGerais: culto.observacoesGerais || ''
    });
  }

  cancelarEdicao(): void {
    this.limparFormulario();
    this.fecharModal();
  }

  async excluir(culto: Culto): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir o culto "${culto.nome}"?`);
    if (!confirmou) {
      return;
    }

    this.mensagemSucesso = '';
    this.mensagemErro = '';
    this.carregando = true;

    try {
      await firstValueFrom(this.cultoService.excluir(culto.id));
      this.mensagemSucesso = 'Culto excluído com sucesso.';

      if (this.cultoEditandoId === culto.id) {
        this.limparFormulario();
      }

      this.carregar();
    } catch (error) {
      this.mensagemErro = this.extrairMensagemErro(error);
    } finally {
      this.carregando = false;
    }
  }

  async gerarDomingosAutomaticamente(): Promise<void> {
    const raw = this.form.getRawValue();
    const quantidade = Number(raw.repeticoesDomingo || 0);

    if (!raw.nome || !raw.tipoCulto || !raw.horarioInicio || !raw.statusCultoId) {
      this.mensagemErro = 'Preencha nome, tipo, horário e status para gerar os próximos domingos.';
      this.mensagemSucesso = '';
      return;
    }

    if (!raw.dataCulto) {
      this.mensagemErro = 'Informe a data inicial para calcular os próximos domingos.';
      this.mensagemSucesso = '';
      return;
    }

    if (!Number.isInteger(quantidade) || quantidade < 1 || quantidade > 52) {
      this.mensagemErro = 'Informe uma quantidade válida entre 1 e 52 domingos.';
      this.mensagemSucesso = '';
      return;
    }

    const base = this.parseDataInput(raw.dataCulto);
    if (!base) {
      this.mensagemErro = 'Data inicial inválida.';
      this.mensagemSucesso = '';
      return;
    }

    const primeiroDomingo = this.proximoOuMesmoDomingo(base);
    const horario = this.normalizarHorario(raw.horarioInicio);
    const nomeNormalizado = raw.nome.trim().toLowerCase();

    this.mensagemSucesso = '';
    this.mensagemErro = '';
    this.carregando = true;

    let criados = 0;
    let ignorados = 0;

    try {
      for (let i = 0; i < quantidade; i += 1) {
        const data = new Date(primeiroDomingo);
        data.setDate(primeiroDomingo.getDate() + i * 7);
        const dataInput = this.formatarDataInput(data);

        const jaExiste = this.cultos.some((culto) => {
          const dataCulto = this.paraDataInput(culto.dataCulto);
          const horarioCulto = this.normalizarHorario(culto.horarioInicio);
          const nomeCulto = culto.nome.trim().toLowerCase();
          return dataCulto === dataInput && horarioCulto === horario && nomeCulto === nomeNormalizado;
        });

        if (jaExiste) {
          ignorados += 1;
          continue;
        }

        await firstValueFrom(
          this.cultoService.criar({
            nome: raw.nome,
            tipoCulto: raw.tipoCulto,
            dataCulto: this.normalizarData(dataInput),
            horarioInicio: horario,
            horarioFimPrevisto: null,
            statusCultoId: raw.statusCultoId,
            observacoesGerais: raw.observacoesGerais || null,
            templateCultoId: null
          })
        );
        criados += 1;
      }

      this.carregar();
      this.mensagemSucesso = `${criados} culto(s) criado(s) automaticamente.`;
      if (ignorados > 0) {
        this.mensagemSucesso += ` ${ignorados} já existiam e foram ignorados.`;
      }
    } catch (error) {
      this.mensagemErro = this.extrairMensagemErro(error);
    } finally {
      this.carregando = false;
    }
  }

  private limparFormulario(): void {
    this.cultoEditandoId = null;
    const repeticoesDomingoAtual = this.form.controls.repeticoesDomingo.value || 8;
    this.form.reset({
      nome: '',
      tipoCulto: 'Culto de Celebração',
      dataCulto: '',
      horarioInicio: '19:00',
      statusCultoId: 1,
      observacoesGerais: '',
      repeticoesDomingo: repeticoesDomingoAtual
    });
  }

  private normalizarHorario(valor: string): string {
    if (!valor) {
      return valor;
    }

    const limpo = valor.trim();
    const match = limpo.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?$/);
    if (match) {
      const hora = match[1].padStart(2, '0');
      const minuto = match[2];
      const segundo = match[3] ?? '00';
      return `${hora}:${minuto}:${segundo}`;
    }

    return limpo.length === 5 ? `${limpo}:00` : limpo;
  }

  private normalizarData(valor: string): string {
    if (!valor) {
      return valor;
    }

    return valor.length === 10 ? `${valor}T00:00:00` : valor;
  }

  private parseDataInput(valor: string): Date | null {
    const partes = valor.split('-').map((item) => Number(item));
    if (partes.length !== 3 || partes.some((item) => Number.isNaN(item))) {
      return null;
    }

    return new Date(partes[0], partes[1] - 1, partes[2]);
  }

  private proximoOuMesmoDomingo(data: Date): Date {
    const resultado = new Date(data);
    const diasAteDomingo = (7 - resultado.getDay()) % 7;
    resultado.setDate(resultado.getDate() + diasAteDomingo);
    return resultado;
  }

  private formatarDataInput(data: Date): string {
    const ano = data.getFullYear();
    const mes = `${data.getMonth() + 1}`.padStart(2, '0');
    const dia = `${data.getDate()}`.padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
  }

  private paraDataInput(valor: string): string {
    if (!valor) {
      return '';
    }

    if (valor.length >= 10 && valor[4] === '-' && valor[7] === '-') {
      return valor.slice(0, 10);
    }

    const data = new Date(valor);
    if (Number.isNaN(data.getTime())) {
      return '';
    }

    return this.formatarDataInput(data);
  }

  paraHorarioInput(valor: unknown): string {
    if (!valor) {
      return '';
    }

    if (typeof valor === 'object') {
      const origem = valor as Record<string, unknown>;
      const hora = origem['hours'] ?? origem['hour'] ?? origem['Hours'] ?? origem['Hour'];
      const minuto = origem['minutes'] ?? origem['minute'] ?? origem['Minutes'] ?? origem['Minute'];

      if (typeof hora === 'number' && typeof minuto === 'number') {
        return `${`${hora}`.padStart(2, '0')}:${`${minuto}`.padStart(2, '0')}`;
      }
    }

    const limpo = String(valor).trim();
    const match = limpo.match(/^(\d{1,2}):(\d{2})(?::\d{2})?/);
    if (match) {
      return `${match[1].padStart(2, '0')}:${match[2]}`;
    }

    return limpo.length >= 5 ? limpo.slice(0, 5) : limpo;
  }

  private extrairMensagemErro(error: any): string {
    if (error?.error?.mensagem) {
      return error.error.mensagem;
    }

    const errors = error?.error?.errors;
    if (errors && typeof errors === 'object') {
      const primeiraChave = Object.keys(errors)[0];
      if (primeiraChave && Array.isArray(errors[primeiraChave]) && errors[primeiraChave].length > 0) {
        return errors[primeiraChave][0];
      }
    }

    return 'Não foi possível salvar o culto.';
  }

  private mapearParaGrid(cultos: Culto[]): CultoGridRow[] {
    return cultos.map((culto) => ({
      nome: culto.nome,
      tipoCulto: culto.tipoCulto,
      dataCulto: this.formatarDataGrid(culto.dataCulto),
      horarioInicio: this.paraHorarioInput(culto.horarioInicio),
      statusCulto: this.statusLabel(culto.statusCultoId),
      observacoesGerais: culto.observacoesGerais || '-',
      culto
    }));
  }

  private formatarDataGrid(valor: string): string {
    const dataInput = this.paraDataInput(valor);
    if (!dataInput) {
      return valor;
    }

    const partes = dataInput.split('-');
    if (partes.length !== 3) {
      return dataInput;
    }

    return `${partes[2]}/${partes[1]}/${partes[0]}`;
  }

  statusLabel(statusId: number): string {
    return this.statusOptions.find((x) => x.id === statusId)?.label ?? 'Sem status';
  }

}
