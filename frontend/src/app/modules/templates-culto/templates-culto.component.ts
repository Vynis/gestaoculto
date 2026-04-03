import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { CellClickedEvent, ColDef } from 'ag-grid-community';
import { Ministerio } from '../../core/models/cadastro.models';
import { TemplateCulto, TemplateCultoRequest, TemplateEtapa, TemplateEtapaMinisterioAcao } from '../../core/models/template-culto.models';
import { CadastroService } from '../../core/services/cadastro.service';
import { TemplateCultoService } from '../../core/services/template-culto.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

interface TemplateAcaoGridRow {
  index: number;
  ordem: number | null;
  ministerio: string;
  descricaoAcao: string;
  observacao: string;
  ativo: string;
}

interface TemplateEtapaGridRow {
  index: number;
  sequencia: number;
  horarioInicialPadrao: string;
  duracaoMinutos: number;
  atividade: string;
  descricao: string;
  ministerioResponsavel: string;
  totalAcoes: number;
}

@Component({
  selector: 'app-templates-culto',
  templateUrl: './templates-culto.component.html',
  styleUrls: ['./templates-culto.component.scss']
})
export class TemplatesCultoComponent implements OnInit {
  templates: TemplateCulto[] = [];
  etapasTemplate: TemplateEtapa[] = [];
  ministerios: Ministerio[] = [];
  carregando = false;
  salvandoTemplate = false;
  modalAberto = false;
  templateEditandoId: number | null = null;
  etapaEditandoIndex: number | null = null;
  etapaAcoesIndex: number | null = null;
  acaoEtapaEditandoIndex: number | null = null;
  acaoModalAberto = false;

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    tipoCulto: ['', Validators.required],
    descricao: [''],
    ativo: [true]
  });

  readonly etapaForm = this.fb.group({
    sequencia: [1, [Validators.required, Validators.min(1)]],
    horarioInicialPadrao: [''],
    duracaoMinutos: [10, [Validators.required, Validators.min(1)]],
    atividade: ['', Validators.required],
    descricao: ['']
  });

  readonly acaoEtapaForm = this.fb.group({
    ministerioId: [null as number | null, [Validators.required]],
    descricaoAcao: ['', Validators.required],
    ordem: [null as number | null],
    observacao: [''],
    ativo: [true]
  });

  readonly acoesGridDefaultColDef: ColDef<TemplateAcaoGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 120
  };

  readonly acoesGridColumnDefs: ColDef<TemplateAcaoGridRow>[] = [
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

  readonly etapasGridDefaultColDef: ColDef<TemplateEtapaGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 120
  };

  readonly etapasGridColumnDefs: ColDef<TemplateEtapaGridRow>[] = [
    { headerName: 'Seq', field: 'sequencia', maxWidth: 90, minWidth: 80 },
    { headerName: 'Horário', field: 'horarioInicialPadrao', minWidth: 120, maxWidth: 140 },
    { headerName: 'Duração', field: 'duracaoMinutos', minWidth: 110, maxWidth: 130 },
    { headerName: 'Atividade', field: 'atividade', minWidth: 220 },
    { headerName: 'Descrição', field: 'descricao', minWidth: 220 },
    { headerName: 'Ministério etapa', field: 'ministerioResponsavel', minWidth: 180 },
    { headerName: 'Ações ministério', field: 'totalAcoes', minWidth: 130, maxWidth: 170 },
    {
      headerName: 'Ações',
      field: 'index',
      minWidth: 150,
      maxWidth: 180,
      sortable: false,
      filter: false,
      cellRenderer: () =>
        '<div class="grid-actions"><button class="grid-btn icon info" data-action="acoes-etapa" type="button" title="Ações por ministério" aria-label="Ações por ministério"><span class="icon-list"></span></button><button class="grid-btn icon edit" data-action="editar-etapa" type="button" title="Editar etapa" aria-label="Editar etapa"><span class="icon-pencil"></span></button><button class="grid-btn icon delete" data-action="excluir-etapa" type="button" title="Excluir etapa" aria-label="Excluir etapa"><span class="icon-trash"></span></button></div>'
    }
  ];

  readonly etapasGridLocaleText = {
    noRowsToShow: 'Nenhuma etapa adicionada ao template.'
  };

  constructor(
    private readonly fb: FormBuilder,
    private readonly templateCultoService: TemplateCultoService,
    private readonly cadastroService: CadastroService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.limparEtapaForm();
    this.cadastroService.listarMinisterios().subscribe((data) => {
      this.ministerios = data;
    });
    this.carregar();
  }

  carregar(): void {
    this.carregando = true;
    this.templateCultoService.listar().subscribe({
      next: (data) => {
        this.templates = data;
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.etapasTemplate.length === 0) {
      this.toastr.warning('Adicione pelo menos uma etapa ao template.', 'Templates');
      return;
    }

    const raw = this.form.getRawValue();
    const etapasNormalizadas: TemplateEtapa[] = this.etapasTemplate
      .map((etapa, etapaIndex) => ({
        id: etapa.id,
        sequencia: etapa.sequencia || etapaIndex + 1,
        horarioInicialPadrao: etapa.horarioInicialPadrao || null,
        duracaoMinutos: etapa.duracaoMinutos || 10,
        atividade: (etapa.atividade || '').trim(),
        descricao: (etapa.descricao || '').trim() || null,
        ministerioIds: (etapa.ministerioIds || (etapa.ministerioResponsavelId ? [etapa.ministerioResponsavelId] : []))
          .filter((id) => Number(id) > 0),
        ministerioResponsavelId: (etapa.ministerioIds || []).length
          ? Number(etapa.ministerioIds[0])
          : (etapa.ministerioResponsavelId ?? null),
        observacoes: (etapa.observacoes || '').trim() || null,
        statusEtapaId: etapa.statusEtapaId ?? 1,
        acoesMinisterio: (etapa.acoesMinisterio || [])
          .filter((acao) => Number(acao.ministerioId) > 0 && !!(acao.descricaoAcao || '').trim())
          .map((acao, acaoIndex) => ({
            id: acao.id,
            templateEtapaCultoId: acao.templateEtapaCultoId,
            ministerioId: Number(acao.ministerioId),
            ordem: acao.ordem ?? (acaoIndex + 1),
            descricaoAcao: (acao.descricaoAcao || '').trim(),
            observacao: (acao.observacao || '').trim() || null,
            ativo: acao.ativo ?? true
          }))
      }))
      .filter((etapa) => !!etapa.atividade && etapa.duracaoMinutos > 0);

    if (!etapasNormalizadas.length) {
      this.toastr.warning('Adicione pelo menos uma etapa válida ao template.', 'Templates');
      return;
    }

    const payload: TemplateCultoRequest = {
      nome: raw.nome || '',
      tipoCulto: raw.tipoCulto || '',
      descricao: raw.descricao || null,
      ativo: raw.ativo ?? true,
      etapas: etapasNormalizadas
    };

    const requisicao = this.templateEditandoId === null
      ? this.templateCultoService.criar(payload)
      : this.templateCultoService.atualizar(this.templateEditandoId, payload);

    this.salvandoTemplate = true;

    requisicao.subscribe({
      next: () => {
        this.toastr.success(
          this.templateEditandoId === null ? 'Template criado com sucesso.' : 'Template atualizado com sucesso.',
          'Templates'
        );
        this.limparFormulario();
        this.fecharModal();
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao salvar template');
        this.salvandoTemplate = false;
      },
      complete: () => {
        this.salvandoTemplate = false;
      }
    });
  }

  editar(template: TemplateCulto): void {
    this.modalAberto = true;
    this.templateEditandoId = template.id;
    this.etapasTemplate = (template.etapas || []).map((etapa) => ({
      ...etapa,
      horarioInicialPadrao: etapa.horarioInicialPadrao || null,
      descricao: etapa.descricao || null,
      observacoes: etapa.observacoes || null,
      statusEtapaId: etapa.statusEtapaId ?? 1,
      ministerioIds: (etapa.ministerioIds || (etapa.ministerioResponsavelId ? [etapa.ministerioResponsavelId] : []))
        .filter((id) => Number(id) > 0),
      ministerioResponsavelId: etapa.ministerioResponsavelId ?? null,
      acoesMinisterio: (etapa.acoesMinisterio || []).map((acao, index) => ({
        ...acao,
        ordem: acao.ordem ?? index + 1,
        observacao: acao.observacao || null,
        ativo: acao.ativo ?? true
      }))
    }));
    this.form.patchValue({
      nome: template.nome,
      tipoCulto: template.tipoCulto,
      descricao: template.descricao || '',
      ativo: template.ativo
    });
  }

  cancelarEdicao(): void {
    this.limparFormulario();
    this.fecharModal();
  }

  abrirModalNovo(): void {
    this.limparFormulario();
    this.modalAberto = true;
  }

  fecharModal(): void {
    this.modalAberto = false;
  }

  salvarEtapa(): void {
    if (this.etapaForm.invalid) {
      this.etapaForm.markAllAsTouched();
      return;
    }

    const raw = this.etapaForm.getRawValue();
    const ministerioIdsExistentes = this.etapaEditandoIndex !== null
      ? (this.etapasTemplate[this.etapaEditandoIndex]?.ministerioIds || []).filter((id) => Number(id) > 0)
      : [];
    const etapa: TemplateEtapa = {
      sequencia: raw.sequencia || 1,
      horarioInicialPadrao: raw.horarioInicialPadrao || null,
      duracaoMinutos: raw.duracaoMinutos || 10,
      atividade: raw.atividade || '',
      descricao: raw.descricao || null,
      ministerioIds: ministerioIdsExistentes,
      ministerioResponsavelId: ministerioIdsExistentes.length ? Number(ministerioIdsExistentes[0]) : null,
      observacoes: null,
      statusEtapaId: 1,
      acoesMinisterio: this.etapaEditandoIndex !== null
        ? (this.etapasTemplate[this.etapaEditandoIndex]?.acoesMinisterio || [])
        : []
    };

    if (this.etapaEditandoIndex === null) {
      this.etapasTemplate.push(etapa);
    } else {
      this.etapasTemplate[this.etapaEditandoIndex] = etapa;
    }

    this.reordenarEtapas();
    this.limparEtapaForm();
  }

  abrirAcoesDaEtapa(index: number): void {
    const etapa = this.etapasTemplate[index];
    if (!etapa) {
      return;
    }

    this.etapaAcoesIndex = index;
    this.acaoModalAberto = true;
    this.cancelarEdicaoAcaoEtapa();
  }

  fecharAcoesDaEtapa(): void {
    this.acaoModalAberto = false;
    this.etapaAcoesIndex = null;
    this.cancelarEdicaoAcaoEtapa();
  }

  editarEtapa(index: number): void {
    const etapa = this.etapasTemplate[index];
    if (!etapa) {
      return;
    }

    this.etapaEditandoIndex = index;
    this.cancelarEdicaoAcaoEtapa();
    this.etapaForm.patchValue({
      sequencia: etapa.sequencia,
      horarioInicialPadrao: etapa.horarioInicialPadrao || '',
      duracaoMinutos: etapa.duracaoMinutos,
      atividade: etapa.atividade,
      descricao: etapa.descricao || ''
    });
  }

  async excluirEtapa(index: number): Promise<void> {
    const etapa = this.etapasTemplate[index];
    const nome = etapa?.atividade || `Etapa #${index + 1}`;
    const confirmou = await confirmarExclusao(`Deseja excluir a etapa "${nome}"?`);
    if (!confirmou) {
      return;
    }

    this.etapasTemplate.splice(index, 1);
    this.reordenarEtapas();

    if (this.etapaEditandoIndex === index) {
      this.limparEtapaForm();
    }

    if (this.etapaAcoesIndex === index) {
      this.fecharAcoesDaEtapa();
    }
  }

  cancelarEdicaoEtapa(): void {
    this.limparEtapaForm();
  }

  salvarAcaoEtapa(): void {
    if (this.etapaAcoesIndex === null) {
      this.toastr.warning('Selecione uma etapa para editar as acoes por ministerio.', 'Templates');
      return;
    }

    if (this.acaoEtapaForm.invalid) {
      this.acaoEtapaForm.markAllAsTouched();
      return;
    }

    const etapa = this.etapasTemplate[this.etapaAcoesIndex];
    if (!etapa) {
      return;
    }

    const raw = this.acaoEtapaForm.getRawValue();
    const acao: TemplateEtapaMinisterioAcao = {
      ministerioId: Number(raw.ministerioId) || 0,
      descricaoAcao: (raw.descricaoAcao || '').trim(),
      ordem: raw.ordem ?? ((etapa.acoesMinisterio || []).length + 1),
      observacao: (raw.observacao || '').trim() || null,
      ativo: raw.ativo ?? true
    };

    const atuais = etapa.acoesMinisterio || [];
    if (this.acaoEtapaEditandoIndex === null) {
      atuais.push(acao);
    } else {
      atuais[this.acaoEtapaEditandoIndex] = acao;
    }

    etapa.acoesMinisterio = this.reordenarAcoes(atuais);
    etapa.ministerioIds = Array.from(new Set((etapa.acoesMinisterio || []).map((x) => x.ministerioId).filter((id) => id > 0)));
    etapa.ministerioResponsavelId = etapa.ministerioIds.length ? etapa.ministerioIds[0] : null;
    this.cancelarEdicaoAcaoEtapa();
  }

  editarAcaoEtapa(index: number): void {
    if (this.etapaAcoesIndex === null) {
      return;
    }

    const etapa = this.etapasTemplate[this.etapaAcoesIndex];
    const item = etapa?.acoesMinisterio?.[index];
    if (!item) {
      return;
    }

    this.acaoEtapaEditandoIndex = index;
    this.acaoEtapaForm.patchValue({
      ministerioId: item.ministerioId,
      descricaoAcao: item.descricaoAcao,
      ordem: item.ordem,
      observacao: item.observacao || '',
      ativo: item.ativo
    });
  }

  async excluirAcaoEtapa(index: number): Promise<void> {
    if (this.etapaAcoesIndex === null) {
      return;
    }

    const etapa = this.etapasTemplate[this.etapaAcoesIndex];
    if (!etapa?.acoesMinisterio) {
      return;
    }

    const acao = etapa.acoesMinisterio[index];
    const descricao = acao?.descricaoAcao || `Ação #${index + 1}`;
    const confirmou = await confirmarExclusao(`Deseja excluir a ação "${descricao}"?`);
    if (!confirmou) {
      return;
    }

    etapa.acoesMinisterio.splice(index, 1);
    etapa.acoesMinisterio = this.reordenarAcoes(etapa.acoesMinisterio);
    etapa.ministerioIds = Array.from(new Set((etapa.acoesMinisterio || []).map((x) => x.ministerioId).filter((id) => id > 0)));
    etapa.ministerioResponsavelId = etapa.ministerioIds.length ? etapa.ministerioIds[0] : null;

    if (this.acaoEtapaEditandoIndex === index) {
      this.cancelarEdicaoAcaoEtapa();
    }
  }

  cancelarEdicaoAcaoEtapa(): void {
    this.acaoEtapaEditandoIndex = null;
    const totalAtual = this.etapaAcoesIndex === null
      ? 0
      : (this.etapasTemplate[this.etapaAcoesIndex]?.acoesMinisterio?.length || 0);

    this.acaoEtapaForm.reset({
      ministerioId: null,
      descricaoAcao: '',
      ordem: totalAtual + 1,
      observacao: '',
      ativo: true
    });
  }

  onAcoesGridCellClicked(event: CellClickedEvent<TemplateAcaoGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data) {
      return;
    }

    if (action === 'editar') {
      this.editarAcaoEtapa(event.data.index);
      return;
    }

    if (action === 'excluir') {
      this.excluirAcaoEtapa(event.data.index);
    }
  }

  onEtapasGridCellClicked(event: CellClickedEvent<TemplateEtapaGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data) {
      return;
    }

    if (action === 'acoes-etapa') {
      this.abrirAcoesDaEtapa(event.data.index);
      return;
    }

    if (action === 'editar-etapa') {
      this.editarEtapa(event.data.index);
      return;
    }

    if (action === 'excluir-etapa') {
      this.excluirEtapa(event.data.index);
    }
  }

  get acoesEtapaGridRowData(): TemplateAcaoGridRow[] {
    if (this.etapaAcoesIndex === null) {
      return [];
    }

    const etapa = this.etapasTemplate[this.etapaAcoesIndex];
    const acoes = etapa?.acoesMinisterio || [];

    return acoes.map((acao, index) => ({
      index,
      ordem: acao.ordem ?? null,
      ministerio: this.nomeMinisterio(acao.ministerioId),
      descricaoAcao: acao.descricaoAcao,
      observacao: acao.observacao || '-',
      ativo: acao.ativo ? 'Sim' : 'Não'
    }));
  }

  get etapasGridRowData(): TemplateEtapaGridRow[] {
    return this.etapasTemplate.map((etapa, index) => ({
      index,
      sequencia: etapa.sequencia,
      horarioInicialPadrao: etapa.horarioInicialPadrao || '-',
      duracaoMinutos: etapa.duracaoMinutos,
      atividade: etapa.atividade,
      descricao: etapa.descricao || '-',
      ministerioResponsavel: (etapa.ministerioIds || []).length
        ? (etapa.ministerioIds || []).map((id) => this.nomeMinisterio(id)).join(', ')
        : (etapa.ministerioResponsavelId ? this.nomeMinisterio(etapa.ministerioResponsavelId) : '-'),
      totalAcoes: etapa.acoesMinisterio?.length || 0
    }));
  }

  async excluir(template: TemplateCulto): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir o template "${template.nome}"?`);
    if (!confirmou) {
      return;
    }

    this.templateCultoService.excluir(template.id).subscribe({
      next: () => {
        if (this.templateEditandoId === template.id) {
          this.limparFormulario();
        }
        this.toastr.success('Template excluído com sucesso.', 'Templates');
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao excluir template');
      }
    });
  }

  duplicar(template: TemplateCulto): void {
    const payload: TemplateCultoRequest = {
      nome: `${template.nome} (Copia)`,
      tipoCulto: template.tipoCulto,
      descricao: template.descricao || null,
      ativo: template.ativo,
      etapas: (template.etapas || []).map((etapa, etapaIndex) => ({
        sequencia: etapaIndex + 1,
        horarioInicialPadrao: etapa.horarioInicialPadrao || null,
        duracaoMinutos: etapa.duracaoMinutos || 10,
        atividade: (etapa.atividade || '').trim(),
        descricao: (etapa.descricao || '').trim() || null,
        ministerioIds: (etapa.ministerioIds || []).filter((id) => Number(id) > 0),
        ministerioResponsavelId: etapa.ministerioResponsavelId ?? null,
        observacoes: (etapa.observacoes || '').trim() || null,
        statusEtapaId: etapa.statusEtapaId ?? 1,
        acoesMinisterio: (etapa.acoesMinisterio || []).map((acao, acaoIndex) => ({
          ministerioId: Number(acao.ministerioId),
          ordem: acao.ordem ?? (acaoIndex + 1),
          descricaoAcao: (acao.descricaoAcao || '').trim(),
          observacao: (acao.observacao || '').trim() || null,
          ativo: acao.ativo ?? true
        }))
      }))
    };

    this.templateCultoService.criar(payload).subscribe({
      next: () => {
        this.toastr.success('Template duplicado com sucesso.', 'Templates');
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(this.extrairMensagemErro(error), 'Erro ao duplicar template');
      }
    });
  }

  private limparFormulario(): void {
    this.templateEditandoId = null;
    this.etapasTemplate = [];
    this.limparEtapaForm();
    this.fecharAcoesDaEtapa();
    this.cancelarEdicaoAcaoEtapa();
    this.form.reset({
      nome: '',
      tipoCulto: '',
      descricao: '',
      ativo: true
    });
  }

  private extrairMensagemErro(error: any): string {
    if (error?.error?.mensagem) {
      return error.error.mensagem;
    }

    return 'Não foi possível concluir a operação.';
  }

  private limparEtapaForm(): void {
    this.etapaEditandoIndex = null;
    this.cancelarEdicaoAcaoEtapa();
    this.etapaForm.reset({
      sequencia: this.etapasTemplate.length + 1,
      horarioInicialPadrao: '',
      duracaoMinutos: 10,
      atividade: '',
      descricao: ''
    });
  }

  private reordenarEtapas(): void {
    this.etapasTemplate = this.etapasTemplate
      .slice()
      .sort((a, b) => a.sequencia - b.sequencia)
      .map((item, index) => ({ ...item, sequencia: index + 1 }));
  }

  private reordenarAcoes(acoes: TemplateEtapaMinisterioAcao[]): TemplateEtapaMinisterioAcao[] {
    return acoes
      .slice()
      .sort((a, b) => (a.ordem ?? Number.MAX_SAFE_INTEGER) - (b.ordem ?? Number.MAX_SAFE_INTEGER))
      .map((item, index) => ({ ...item, ordem: index + 1 }));
  }

  nomeMinisterio(id: number): string {
    return this.ministerios.find((x) => x.id === id)?.nome || `Ministerio #${id}`;
  }
}
