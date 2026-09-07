import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CellClickedEvent, ColDef } from 'ag-grid-community';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { EtapaCulto, EtapaMinisterioAcao } from '../../core/models/cronograma.models';
import { CronogramaService } from '../../core/services/cronograma.service';
import { Culto } from '../../core/models/culto.models';
import { CultoService } from '../../core/services/culto.service';
import { CadastroService } from '../../core/services/cadastro.service';
import { Ministerio } from '../../core/models/cadastro.models';
import { NbToastrService } from '@nebular/theme';
import { TemplateCulto } from '../../core/models/template-culto.models';
import { TemplateCultoService } from '../../core/services/template-culto.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

interface AcaoMinisterioGridRow {
  index: number;
  ordem: number | null;
  ministerio: string;
  descricaoAcao: string;
  observacao: string;
  ativo: string;
}

interface CronogramaBlocoView {
  nome: string;
  etapas: EtapaCulto[];
}

@Component({
  selector: 'app-cronograma',
  templateUrl: './cronograma.component.html',
  styleUrls: ['./cronograma.component.scss']
})
export class CronogramaComponent implements OnInit {
  etapas: EtapaCulto[] = [];
  cultos: Culto[] = [];
  ministerios: Ministerio[] = [];
  templates: TemplateCulto[] = [];
  modalAberto = false;
  etapaEditandoId: number | null = null;
  acoesMinisterio: EtapaMinisterioAcao[] = [];
  acaoEditandoIndex: number | null = null;
  aplicandoTemplate = false;
  carregando = false;
  templateSelecionadoId: number | null = null;
  modoOrganizacao = false;
  salvandoOrganizacao = false;
  private etapasOriginais: EtapaCulto[] | null = null;
  readonly filtro = this.fb.group({
    cultoId: [0, [Validators.required, Validators.min(1)]]
  });

  readonly form = this.fb.group({
    cultoId: [0, [Validators.required, Validators.min(1)]],
    sequencia: [1, [Validators.required, Validators.min(1)]],
    horarioInicio: ['', Validators.required],
    duracaoMinutos: [10, [Validators.required, Validators.min(1)]],
    atividade: ['', Validators.required],
    blocoCronograma: ['', Validators.required],
    descricao: [''],
    ministerioResponsavelId: [null as number | null]
  });

  readonly acaoForm = this.fb.group({
    ministerioId: [null as number | null, [Validators.required]],
    descricaoAcao: ['', Validators.required],
    ordem: [null as number | null],
    observacao: [''],
    ativo: [true]
  });

  readonly acoesGridDefaultColDef: ColDef<AcaoMinisterioGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 120
  };

  readonly acoesGridColumnDefs: ColDef<AcaoMinisterioGridRow>[] = [
    { headerName: 'Ordem', field: 'ordem', maxWidth: 110, minWidth: 90 },
    { headerName: 'Ministério', field: 'ministerio', minWidth: 160 },
    { headerName: 'Ação', field: 'descricaoAcao', minWidth: 240 },
    { headerName: 'Observação', field: 'observacao', minWidth: 200 },
    { headerName: 'Ativo', field: 'ativo', maxWidth: 100, minWidth: 90 },
    {
      headerName: 'Ações',
      field: 'index',
      minWidth: 110,
      maxWidth: 130,
      sortable: false,
      filter: false,
      cellRenderer: () =>
        '<div class="grid-actions"><button class="grid-btn icon edit" data-action="editar" type="button" title="Editar ação" aria-label="Editar ação"><span class="icon-pencil"></span></button><button class="grid-btn icon delete" data-action="excluir" type="button" title="Excluir ação" aria-label="Excluir ação"><span class="icon-trash"></span></button></div>'
    }
  ];

  readonly acoesGridLocaleText = {
    noRowsToShow: 'Nenhuma ação por ministério cadastrada.'
  };

  constructor(
    private readonly fb: FormBuilder,
    private readonly cronogramaService: CronogramaService,
    private readonly cultoService: CultoService,
    private readonly cadastroService: CadastroService,
    private readonly templateCultoService: TemplateCultoService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.filtro.controls.cultoId.valueChanges.subscribe((valor) => {
      const cultoId = Number(valor || 0);
      if (!cultoId) {
        this.etapas = [];
        return;
      }

      this.carregar();
    });

    this.cultoService.listarAtivos().subscribe((data) => {
      this.cultos = data;
    });

    this.templateCultoService.listar().subscribe((data) => {
      this.templates = data.filter((item) => item.ativo && item.etapas && item.etapas.length > 0);
      if (!this.templateSelecionadoId && this.templates.length > 0) {
        this.templateSelecionadoId = this.templates[0].id;
      }
    });

    this.cadastroService.listarMinisterios().subscribe((data) => {
      this.ministerios = data;
    });
  }

  carregar(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      return;
    }

    this.form.patchValue({ cultoId });
    this.carregando = true;

    this.cronogramaService.listarPorCulto(cultoId).subscribe((data) => {
      const etapasUnicasPorAssinatura = new Map<string, EtapaCulto>();

      (data || [])
        .filter((item) => item.cultoId === cultoId)
        .filter((item, index, arr) => arr.findIndex((x) => x.id === item.id) === index)
        .sort((a, b) => a.sequencia - b.sequencia || a.id - b.id)
        .forEach((item) => {
          const etapaNormalizada = {
            ...item,
            acoesMinisterio: item.acoesMinisterio || []
          };
          const chave = this.assinaturaEtapa(etapaNormalizada);
          if (!etapasUnicasPorAssinatura.has(chave)) {
            etapasUnicasPorAssinatura.set(chave, etapaNormalizada);
          }
        });

      this.etapas = Array.from(etapasUnicasPorAssinatura.values())
        .sort((a, b) => a.sequencia - b.sequencia || a.id - b.id);
      this.carregando = false;
    }, () => {
      this.carregando = false;
    });
  }

  get gruposCronograma(): CronogramaBlocoView[] {
    const grupos = new Map<string, EtapaCulto[]>();
    for (const etapa of this.etapas) {
      const bloco = this.normalizarBlocoCronograma(etapa.blocoCronograma);
      if (!grupos.has(bloco)) {
        grupos.set(bloco, []);
      }
      grupos.get(bloco)!.push(etapa);
    }

    return Array.from(grupos.entries())
      .sort(([a], [b]) => a === 'PRINCIPAL' ? -1 : b === 'PRINCIPAL' ? 1 : a.localeCompare(b, 'pt-BR'))
      .map(([nome, etapas]) => ({
        nome,
        etapas: etapas.slice().sort((a, b) => a.sequencia - b.sequencia || a.id - b.id)
      }));
  }

  iniciarOrganizacao(): void {
    if (!this.etapas.length) {
      this.toastr.warning('Não há etapas para organizar.', 'Cronograma');
      return;
    }

    this.etapasOriginais = this.clonarEtapas(this.etapas);
    this.modoOrganizacao = true;
  }

  cancelarOrganizacao(): void {
    if (this.etapasOriginais) {
      this.etapas = this.clonarEtapas(this.etapasOriginais);
    }
    this.etapasOriginais = null;
    this.modoOrganizacao = false;
  }

  aoSoltarEtapa(event: CdkDragDrop<EtapaCulto[]>, grupo: CronogramaBlocoView): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }

    moveItemInArray(grupo.etapas, event.previousIndex, event.currentIndex);
    this.aplicarOrdemDoGrupo(grupo.nome, grupo.etapas);
  }

  moverEtapa(grupo: CronogramaBlocoView, indice: number, deslocamento: number): void {
    const novoIndice = indice + deslocamento;
    if (novoIndice < 0 || novoIndice >= grupo.etapas.length) {
      return;
    }

    moveItemInArray(grupo.etapas, indice, novoIndice);
    this.aplicarOrdemDoGrupo(grupo.nome, grupo.etapas);
  }

  salvarOrganizacao(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId || !this.modoOrganizacao) {
      return;
    }

    this.salvandoOrganizacao = true;
    this.cronogramaService.reordenar(cultoId, {
      blocos: this.gruposCronograma.map((grupo) => ({
        blocoCronograma: grupo.nome,
        etapaIds: grupo.etapas.map((etapa) => etapa.id)
      }))
    }).subscribe({
      next: (response) => {
        this.modoOrganizacao = false;
        this.etapasOriginais = null;
        this.salvandoOrganizacao = false;
        this.carregar();
        this.toastr.success(response.mensagem, 'Cronograma');
      },
      error: (error) => {
        this.salvandoOrganizacao = false;
        this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao organizar cronograma');
      }
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload = {
      cultoId: raw.cultoId ?? 0,
      sequencia: raw.sequencia ?? 1,
      horarioInicio: this.normalizarHorarioInicio(raw.horarioInicio ?? ''),
      duracaoMinutos: raw.duracaoMinutos ?? 10,
      atividade: raw.atividade ?? '',
      blocoCronograma: this.normalizarBlocoCronograma(raw.blocoCronograma ?? 'PRINCIPAL'),
      descricao: raw.descricao ?? null,
      ministerioResponsavelId: raw.ministerioResponsavelId ?? null,
      statusEtapaId: 1,
      acoesMinisterio: this.acoesMinisterio.map((acao, index) => ({
        ...acao,
        ordem: acao.ordem ?? index + 1
      }))
    };

    const requisicao = this.etapaEditandoId === null
      ? this.cronogramaService.criar(payload)
      : this.cronogramaService.atualizar(this.etapaEditandoId, payload);

    requisicao.subscribe({
      next: () => {
        this.filtro.patchValue({ cultoId: Number(this.form.value.cultoId) }, { emitEvent: false });
        this.carregar();
        this.toastr.success(
          this.etapaEditandoId === null ? 'Etapa adicionada ao cronograma.' : 'Etapa atualizada com sucesso.',
          'Sucesso'
        );
        this.limparFormularioEtapa();
        this.fecharModal();
      },
      error: (error) => {
        this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao salvar etapa');
      }
    });
  }

  editarEtapa(etapa: EtapaCulto): void {
    this.modalAberto = true;
    this.etapaEditandoId = etapa.id;
    this.form.patchValue({
      cultoId: etapa.cultoId,
      sequencia: etapa.sequencia,
      horarioInicio: this.paraHoraInput(etapa.horarioInicio),
      duracaoMinutos: etapa.duracaoMinutos,
      atividade: etapa.atividade,
      blocoCronograma: this.normalizarBlocoCronograma(etapa.blocoCronograma),
      descricao: etapa.descricao || '',
      ministerioResponsavelId: etapa.ministerioResponsavelId ?? null
    });

    this.acoesMinisterio = (etapa.acoesMinisterio || []).map((acao, index) => ({
      ...acao,
      ordem: acao.ordem ?? index + 1,
      observacao: acao.observacao || null,
      ativo: acao.ativo ?? true
    }));
    this.cancelarEdicaoAcao();
  }

  cancelarEdicaoEtapa(): void {
    this.limparFormularioEtapa();
    this.fecharModal();
  }

  abrirModalNovaEtapa(): void {
    this.limparFormularioEtapa();
    this.modalAberto = true;
  }

  fecharModal(): void {
    this.modalAberto = false;
  }

  async excluirEtapa(etapa: EtapaCulto): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir a etapa "${etapa.atividade}"?`);
    if (!confirmou) {
      return;
    }

    this.cronogramaService.excluir(etapa.id).subscribe({
      next: () => {
        if (this.etapaEditandoId === etapa.id) {
          this.limparFormularioEtapa();
        }
        this.carregar();
        this.toastr.success('Etapa excluída com sucesso.', 'Cronograma');
      },
      error: (error) => {
        this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao excluir etapa');
      }
    });
  }

  async excluirTodasEtapasDoCulto(): Promise<void> {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.toastr.warning('Selecione um culto antes de excluir.', 'Cronograma');
      return;
    }

    const nomeCulto = this.cultos.find((item) => item.id === cultoId)?.nome || `#${cultoId}`;
    const confirmou = await confirmarExclusao(`Deseja excluir todas as etapas do culto "${nomeCulto}"?`);
    if (!confirmou) {
      return;
    }

    this.cronogramaService.excluirTodasDoCulto(cultoId).subscribe({
      next: (response) => {
        this.etapas = [];
        this.limparFormularioEtapa();
        this.toastr.success(response.mensagem, 'Cronograma');
      },
      error: (error) => {
        this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao excluir etapas');
      }
    });
  }

  async aplicarTemplateRapido(): Promise<void> {
    const cultoId = Number(this.filtro.value.cultoId || this.form.value.cultoId || 0);
    if (!cultoId || !this.templateSelecionadoId) {
      this.toastr.warning('Selecione culto e template para aplicar.', 'Template');
      return;
    }

    this.filtro.patchValue({ cultoId });
    this.form.patchValue({ cultoId });

    const culto = this.cultos.find((item) => item.id === cultoId);
    const template = this.templates.find((item) => item.id === this.templateSelecionadoId);

    if (!culto || !template) {
      this.toastr.warning('Template ou culto inválido.', 'Template');
      return;
    }

    this.aplicandoTemplate = true;

    try {
      const inicioBase = this.dataHoraBaseCulto(culto);
      const sequenciaInicial = this.etapas.length > 0 ? Math.max(...this.etapas.map((item) => item.sequencia)) + 1 : 1;
      let acumuladoMinutos = 0;

      for (let i = 0; i < template.etapas.length; i += 1) {
        const etapaTemplate = template.etapas[i];
        const inicioEtapa = etapaTemplate.horarioInicialPadrao
          ? this.dataHoraPorHorarioTemplate(culto, etapaTemplate.horarioInicialPadrao)
          : new Date(inicioBase);

        if (!etapaTemplate.horarioInicialPadrao) {
          inicioEtapa.setMinutes(inicioEtapa.getMinutes() + acumuladoMinutos);
        }

        await firstValueFrom(
          this.cronogramaService.criar({
            cultoId,
            sequencia: sequenciaInicial + i,
            horarioInicio: this.formatarDateTimeCompleto(inicioEtapa),
            duracaoMinutos: etapaTemplate.duracaoMinutos,
            atividade: etapaTemplate.atividade,
            blocoCronograma: this.normalizarBlocoCronograma(etapaTemplate.blocoCronograma || 'PRINCIPAL'),
            descricao: etapaTemplate.descricao,
            ministerioResponsavelId: (etapaTemplate.ministerioIds || []).length
              ? etapaTemplate.ministerioIds[0]
              : etapaTemplate.ministerioResponsavelId,
            statusEtapaId: 1,
            acoesMinisterio: (etapaTemplate.acoesMinisterio || []).map((acao, idx) => ({
              ministerioId: acao.ministerioId,
              ordem: acao.ordem ?? idx + 1,
              descricaoAcao: acao.descricaoAcao,
              observacao: acao.observacao,
              ativo: acao.ativo
            }))
          })
        );

        acumuladoMinutos += etapaTemplate.duracaoMinutos;
      }

      this.carregar();
      this.toastr.success(`${template.etapas.length} etapa(s) adicionada(s) a partir do template.`, 'Template aplicado');
      this.limparFormularioEtapa();
    } catch (error) {
      this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao aplicar template');
    } finally {
      this.aplicandoTemplate = false;
    }
  }

  private normalizarHorarioInicio(valor: string): string {
    if (!valor) {
      return valor;
    }

    const limpo = valor.trim();
    const apenasHora = /^(\d{1,2}):(\d{2})(?::(\d{2}))?$/.test(limpo);
    if (apenasHora) {
      const partes = limpo.split(':');
      const hora = partes[0].padStart(2, '0');
      const minuto = partes[1].padStart(2, '0');
      const segundo = (partes[2] || '00').padStart(2, '0');
      return `0001-01-01T${hora}:${minuto}:${segundo}`;
    }

    const temDataCompleta = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2})?$/.test(limpo);
    if (temDataCompleta) {
      return limpo.length === 16 ? `${limpo}:00` : limpo;
    }

    return limpo;
  }

  private limparFormularioEtapa(): void {
    this.etapaEditandoId = null;
    this.acoesMinisterio = [];
    this.cancelarEdicaoAcao();
    const cultoIdAtual = Number(this.filtro.value.cultoId) || Number(this.form.value.cultoId) || 0;
    this.form.reset({
      cultoId: cultoIdAtual,
      sequencia: this.etapas.length > 0 ? Math.max(...this.etapas.map((item) => item.sequencia)) + 1 : 1,
      horarioInicio: '',
      duracaoMinutos: 10,
      atividade: '',
      blocoCronograma: '',
      descricao: '',
      ministerioResponsavelId: null
    });
  }

  get blocosCronogramaDisponiveis(): string[] {
    const base = ['PRINCIPAL', 'LOUNGE', 'ADICIONAL'];
    const dinamicos = this.etapas
      .map((item) => this.normalizarBlocoCronograma(item.blocoCronograma || 'PRINCIPAL'))
      .filter((item) => !!item);

    return Array.from(new Set([...base, ...dinamicos]))
      .sort((a, b) => a.localeCompare(b, 'pt-BR'));
  }

  get ministeriosOrdenados(): Ministerio[] {
    return (this.ministerios || [])
      .slice()
      .sort((a, b) => String(a.nome || '').localeCompare(String(b.nome || ''), 'pt-BR', { sensitivity: 'base' }));
  }

  salvarAcaoMinisterio(): void {
    if (this.acaoForm.invalid) {
      this.acaoForm.markAllAsTouched();
      return;
    }

    const raw = this.acaoForm.getRawValue();
    const acao: EtapaMinisterioAcao = {
      ministerioId: Number(raw.ministerioId) || 0,
      ordem: raw.ordem ?? (this.acoesMinisterio.length + 1),
      descricaoAcao: (raw.descricaoAcao || '').trim(),
      observacao: (raw.observacao || '').trim() || null,
      ativo: raw.ativo ?? true
    };

    if (this.acaoEditandoIndex === null) {
      this.acoesMinisterio.push(acao);
    } else {
      this.acoesMinisterio[this.acaoEditandoIndex] = acao;
    }

    this.reordenarAcoes();
    this.cancelarEdicaoAcao();
  }

  editarAcaoMinisterio(index: number): void {
    const item = this.acoesMinisterio[index];
    if (!item) {
      return;
    }

    this.acaoEditandoIndex = index;
    this.acaoForm.patchValue({
      ministerioId: item.ministerioId,
      descricaoAcao: item.descricaoAcao,
      ordem: item.ordem,
      observacao: item.observacao || '',
      ativo: item.ativo
    });
  }

  async excluirAcaoMinisterio(index: number): Promise<void> {
    const acao = this.acoesMinisterio[index];
    const descricao = acao?.descricaoAcao || `Ação #${index + 1}`;
    const confirmou = await confirmarExclusao(`Deseja excluir a ação "${descricao}"?`);
    if (!confirmou) {
      return;
    }

    this.acoesMinisterio.splice(index, 1);
    this.reordenarAcoes();

    if (this.acaoEditandoIndex === index) {
      this.cancelarEdicaoAcao();
    }
  }

  cancelarEdicaoAcao(): void {
    this.acaoEditandoIndex = null;
    this.acaoForm.reset({
      ministerioId: null,
      descricaoAcao: '',
      ordem: this.acoesMinisterio.length + 1,
      observacao: '',
      ativo: true
    });
  }

  nomeMinisterio(id: number): string {
    return this.ministerios.find((x) => x.id === id)?.nome || `Ministério #${id}`;
  }

  onAcaoGridCellClicked(event: CellClickedEvent<AcaoMinisterioGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data) {
      return;
    }

    if (action === 'editar') {
      this.editarAcaoMinisterio(event.data.index);
      return;
    }

    if (action === 'excluir') {
      this.excluirAcaoMinisterio(event.data.index);
    }
  }

  get acoesMinisterioRowData(): AcaoMinisterioGridRow[] {
    return this.acoesMinisterio.map((acao, index) => ({
      index,
      ordem: acao.ordem ?? null,
      ministerio: this.nomeMinisterio(acao.ministerioId),
      descricaoAcao: acao.descricaoAcao,
      observacao: acao.observacao || '-',
      ativo: acao.ativo ? 'Sim' : 'Não'
    }));
  }

  private reordenarAcoes(): void {
    this.acoesMinisterio = this.acoesMinisterio
      .slice()
      .sort((a, b) => (a.ordem ?? Number.MAX_SAFE_INTEGER) - (b.ordem ?? Number.MAX_SAFE_INTEGER))
      .map((item, index) => ({ ...item, ordem: index + 1 }));
  }

  private paraHoraInput(valor: string): string {
    if (!valor) {
      return '';
    }

    const data = new Date(valor);
    if (!Number.isNaN(data.getTime())) {
      const hora = `${data.getHours()}`.padStart(2, '0');
      const minuto = `${data.getMinutes()}`.padStart(2, '0');
      return `${hora}:${minuto}`;
    }

    const texto = String(valor).trim();
    const match = texto.match(/^(\d{1,2}):(\d{2})/);
    if (!match) {
      return '';
    }

    return `${match[1].padStart(2, '0')}:${match[2]}`;
  }

  private dataHoraBaseCulto(culto: Culto): Date {
    const data = this.extrairDataCulto(culto?.dataCulto);
    const horario = this.extrairHorarioCulto(culto?.horarioInicio) || '19:00';
    const combinada = `${data}T${horario}:00`;
    const resultado = new Date(combinada);
    return Number.isNaN(resultado.getTime()) ? new Date() : resultado;
  }

  private dataHoraPorHorarioTemplate(culto: Culto, horario: string): Date {
    const baseData = this.extrairDataCulto(culto?.dataCulto);
    const horaMinuto = horario.length >= 5 ? horario.slice(0, 5) : horario;
    const valor = `${baseData}T${horaMinuto}:00`;
    const data = new Date(valor);
    return Number.isNaN(data.getTime()) ? this.dataHoraBaseCulto(culto) : data;
  }

  private extrairDataCulto(valor: unknown): string {
    if (!valor) {
      return '';
    }

    if (typeof valor === 'string') {
      return valor.length >= 10 ? valor.slice(0, 10) : valor;
    }

    const data = new Date(String(valor));
    if (Number.isNaN(data.getTime())) {
      return '';
    }

    const ano = data.getFullYear();
    const mes = `${data.getMonth() + 1}`.padStart(2, '0');
    const dia = `${data.getDate()}`.padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
  }

  private extrairHorarioCulto(valor: unknown): string {
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

    const texto = String(valor).trim();
    const match = texto.match(/^(\d{1,2}):(\d{2})/);
    if (!match) {
      return '';
    }

    return `${match[1].padStart(2, '0')}:${match[2]}`;
  }

  private formatarDateTimeCompleto(data: Date): string {
    const ano = data.getFullYear();
    const mes = `${data.getMonth() + 1}`.padStart(2, '0');
    const dia = `${data.getDate()}`.padStart(2, '0');
    const hora = `${data.getHours()}`.padStart(2, '0');
    const minuto = `${data.getMinutes()}`.padStart(2, '0');
    const segundo = `${data.getSeconds()}`.padStart(2, '0');
    return `${ano}-${mes}-${dia}T${hora}:${minuto}:${segundo}`;
  }

  private assinaturaEtapa(item: EtapaCulto): string {
    const sequencia = Number(item.sequencia || 0);
    const bloco = this.normalizarBlocoCronograma(item.blocoCronograma || 'PRINCIPAL').toLowerCase();
    const atividade = String(item.atividade || '').trim().toLowerCase();
    return `${bloco}|${sequencia}|${atividade}`;
  }

  private aplicarOrdemDoGrupo(nome: string, etapasOrdenadas: EtapaCulto[]): void {
    const inicio = etapasOrdenadas
      .map((etapa) => this.dataHoraEtapa(etapa))
      .sort((a, b) => a.getTime() - b.getTime())[0];

    if (!inicio || Number.isNaN(inicio.getTime())) {
      return;
    }

    let horarioAtual = new Date(inicio);
    for (let index = 0; index < etapasOrdenadas.length; index += 1) {
      const etapa = etapasOrdenadas[index];
      const duracao = Math.max(1, Number(etapa.duracaoMinutos) || 1);
      const fim = new Date(horarioAtual);
      fim.setMinutes(fim.getMinutes() + duracao);
      etapa.blocoCronograma = nome;
      etapa.sequencia = index + 1;
      etapa.horarioInicio = this.formatarDateTimeCompleto(horarioAtual);
      etapa.horarioFimCalculado = this.formatarDateTimeCompleto(fim);
      horarioAtual = fim;
    }

    this.etapas = [...this.etapas];
  }

  private dataHoraEtapa(etapa: EtapaCulto): Date {
    const valor = new Date(etapa.horarioInicio);
    if (!Number.isNaN(valor.getTime())) {
      return valor;
    }

    const match = String(etapa.horarioInicio || '').match(/(\d{1,2}):(\d{2})/);
    const resultado = new Date();
    if (match) {
      resultado.setHours(Number(match[1]), Number(match[2]), 0, 0);
    }
    return resultado;
  }

  private clonarEtapas(etapas: EtapaCulto[]): EtapaCulto[] {
    return etapas.map((etapa) => ({
      ...etapa,
      acoesMinisterio: (etapa.acoesMinisterio || []).map((acao) => ({ ...acao }))
    }));
  }

  private normalizarBlocoCronograma(valor: string | null | undefined): string {
    const texto = String(valor || '').trim();
    return texto ? texto.toUpperCase() : 'PRINCIPAL';
  }

  private extrairMensagemErro(error: any): string {
    if (error?.error?.mensagem) {
      return error.error.mensagem;
    }

    return 'Não foi possível concluir a operação do cronograma.';
  }
}
