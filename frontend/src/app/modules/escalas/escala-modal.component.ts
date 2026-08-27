import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { Culto } from '../../core/models/culto.models';
import { Ministerio, Voluntario } from '../../core/models/cadastro.models';
import { Escala } from '../../core/models/escala.models';
import { EscalaService } from '../../core/services/escala.service';
import { CadastroService } from '../../core/services/cadastro.service';
import { CronogramaService } from '../../core/services/cronograma.service';
import { EtapaCulto } from '../../core/models/cronograma.models';

export interface EscalaModalPrefill {
  cultoId: number;
  etapaCultoId?: number | null;
  voluntarioId?: number | null;
  ministerioId?: number | null;
  funcao?: string | null;
  observacoes?: string | null;
}

@Component({
  selector: 'app-escala-modal',
  templateUrl: './escala-modal.component.html',
  styleUrls: ['./escala-modal.component.scss']
})
export class EscalaModalComponent implements OnChanges {
  @Input() aberto = false;
  @Input() cultos: Culto[] = [];
  @Input() voluntarios: Voluntario[] = [];
  @Input() carregandoVoluntarios = false;
  @Input() erroVoluntarios: string | null = null;
  @Input() ministerios: Ministerio[] = [];
  @Input() escalaEditando: Escala | null = null;
  @Input() prefill: EscalaModalPrefill | null = null;

  @Output() fechar = new EventEmitter<void>();
  @Output() salvou = new EventEmitter<void>();
  @Output() recarregarVoluntarios = new EventEmitter<void>();

  ministeriosDisponiveis: Ministerio[] = [];
  funcoesPadraoDisponiveis: string[] = [];
  etapasCulto: EtapaCulto[] = [];
  cadastroVoluntarioAberto = false;
  salvando = false;
  carregandoEtapas = false;
  erroEtapas: string | null = null;
  carregandoVoluntariosFallback = false;
  erroVoluntariosFallback: string | null = null;

  private tentouCarregarVoluntariosFallback = false;

  readonly form = this.fb.group({
    cultoId: [0, Validators.required],
    etapaCultoId: [null as number | null],
    tipoVoluntario: ['CADASTRADO' as 'CADASTRADO' | 'AVULSO'],
    voluntarioId: [null as number | null],
    voluntarioAvulsoNome: [''],
    voluntarioAvulsoTelefone: [''],
    ministerioId: [{ value: null as number | null, disabled: true }],
    funcao: ['', Validators.required],
    presencaStatusId: [1, Validators.required],
    observacoes: ['']
  });

  readonly novoVoluntarioForm = this.fb.group({
    nome: ['', Validators.required],
    telefone: [''],
    email: [''],
    ministerioIds: [[] as number[]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly escalaService: EscalaService,
    private readonly cadastroService: CadastroService,
    private readonly cronogramaService: CronogramaService,
    private readonly toastr: NbToastrService
  ) {
    this.form.controls.cultoId.valueChanges.subscribe((cultoId) => {
      this.carregarEtapasDoCulto(Number(cultoId) || 0);
    });

    this.form.controls.etapaCultoId.valueChanges.subscribe((etapaCultoId) => {
      this.sugerirMinisterioDaEtapa(Number(etapaCultoId) || 0);
    });

    this.form.controls.ministerioId.valueChanges.subscribe((ministerioId) => {
      this.atualizarFuncoesPorMinisterio(Number(ministerioId) || 0);
    });

    this.form.controls.tipoVoluntario.valueChanges.subscribe(() => {
      this.aplicarModoVoluntario();
    });

    this.form.controls.voluntarioId.valueChanges.subscribe((voluntarioId) => {
      if (this.modoVoluntarioAvulso) {
        return;
      }
      this.atualizarMinisteriosDisponiveis(Number(voluntarioId) || 0);
      this.atualizarEstadoControles();
      this.sugerirMinisterioDaEtapa(Number(this.form.controls.etapaCultoId.value) || 0);
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    const abriuModal = !!changes['aberto']?.currentValue && !changes['aberto']?.previousValue;
    const mudouContexto = !!changes['escalaEditando'] || !!changes['prefill'];

    if (abriuModal || (this.aberto && mudouContexto)) {
      if (abriuModal) {
        this.tentouCarregarVoluntariosFallback = false;
        this.erroVoluntariosFallback = null;
      }
      this.cadastroVoluntarioAberto = false;
      this.aplicarContextoFormulario();
      this.garantirVoluntariosDisponiveis();
      return;
    }

    if (!this.aberto) {
      return;
    }

    if (changes['voluntarios'] || changes['ministerios']) {
      this.sincronizarSelecaoComDados();
      this.sugerirMinisterioDaEtapa(Number(this.form.controls.etapaCultoId.value) || 0);
      this.atualizarFuncoesPorMinisterio(Number(this.form.controls.ministerioId.value) || 0);
      this.garantirVoluntariosDisponiveis();
      return;
    }

    if (changes['carregandoVoluntarios'] || changes['erroVoluntarios']) {
      this.atualizarEstadoControles();
      this.garantirVoluntariosDisponiveis();
    }
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const nomeAvulso = (raw.voluntarioAvulsoNome || '').trim();
    const tipoVoluntario = raw.tipoVoluntario || 'CADASTRADO';
    const voluntarioId = tipoVoluntario === 'CADASTRADO'
      ? (raw.voluntarioId ?? null)
      : null;

    if (tipoVoluntario === 'CADASTRADO' && !(Number(voluntarioId) > 0)) {
      this.toastr.warning('Selecione um voluntário cadastrado.', 'Escalas');
      return;
    }

    if (tipoVoluntario === 'AVULSO' && !nomeAvulso) {
      this.toastr.warning('Informe o nome do voluntário avulso.', 'Escalas');
      return;
    }

    const payload = {
      cultoId: raw.cultoId ?? 0,
      etapaCultoId: raw.etapaCultoId ?? null,
      voluntarioId,
      voluntarioAvulsoNome: tipoVoluntario === 'AVULSO' ? nomeAvulso : null,
      voluntarioAvulsoTelefone: tipoVoluntario === 'AVULSO' ? ((raw.voluntarioAvulsoTelefone || '').trim() || null) : null,
      ministerioId: raw.ministerioId ?? null,
      funcao: raw.funcao ?? '',
      presencaStatusId: raw.presencaStatusId ?? 1,
      observacoes: raw.observacoes ?? null
    };

    const requisicao = this.escalaEditando
      ? this.escalaService.atualizar(this.escalaEditando.id, payload)
      : this.escalaService.criar(payload);

    this.salvando = true;
    requisicao.subscribe({
      next: () => {
        this.toastr.success(
          this.escalaEditando ? 'Escala atualizada com sucesso.' : 'Voluntário escalado com sucesso.',
          'Escalas'
        );
        this.salvou.emit();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar escala.', 'Erro');
      },
      complete: () => {
        this.salvando = false;
      }
    });
  }

  abrirCadastroVoluntarioRapido(): void {
    this.cadastroVoluntarioAberto = true;
    this.novoVoluntarioForm.reset({ nome: '', telefone: '', email: '', ministerioIds: [] });
  }

  cancelarCadastroVoluntarioRapido(): void {
    this.cadastroVoluntarioAberto = false;
  }

  salvarNovoVoluntario(): void {
    if (this.novoVoluntarioForm.invalid) {
      this.novoVoluntarioForm.markAllAsTouched();
      return;
    }

    const raw = this.novoVoluntarioForm.getRawValue();
    const idsBrutos = Array.isArray(raw.ministerioIds) ? raw.ministerioIds : [raw.ministerioIds];
    const ministerioIds = Array.from(new Set(idsBrutos
      .map((id) => Number(id))
      .filter((id) => id > 0)));

    const payload: Voluntario = {
      id: 0,
      usuarioId: null,
      nome: (raw.nome || '').trim(),
      telefone: (raw.telefone || '').trim() || null,
      email: (raw.email || '').trim() || null,
      ministerioPrincipalId: ministerioIds.length ? ministerioIds[0] : null,
      ministerioIds,
      observacoes: null,
      restricoesIndisponibilidade: null,
      ativo: true
    };

    this.cadastroService.criarVoluntario(payload).subscribe({
      next: (novo) => {
        this.toastr.success('Voluntário cadastrado com sucesso.', 'Escalas');
        this.cadastroVoluntarioAberto = false;
        this.erroVoluntariosFallback = null;
        this.voluntarios = [...this.voluntarios, novo].sort((a, b) => (a.nome || '').localeCompare(b.nome || '', 'pt-BR', { sensitivity: 'base' }));
        this.form.patchValue({ voluntarioId: novo.id });
        this.atualizarMinisteriosDisponiveis(novo.id);
        this.atualizarEstadoControles();
        this.recarregarVoluntarios.emit();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível cadastrar voluntário.', 'Erro');
      }
    });
  }

  tentarNovamenteCarregarVoluntarios(): void {
    this.tentouCarregarVoluntariosFallback = false;
    this.recarregarVoluntarios.emit();
    this.garantirVoluntariosDisponiveis(true);
  }

  fecharModal(): void {
    this.fechar.emit();
  }

  private sincronizarSelecaoComDados(): void {
    const voluntarioSelecionado = Number(this.form.controls.voluntarioId.value) || 0;
    const voluntarioValido = this.existeVoluntario(voluntarioSelecionado);
    const fallbackVoluntarioId = this.obterVoluntarioPreferencial();

    const voluntarioBase = voluntarioValido ? voluntarioSelecionado : fallbackVoluntarioId;
    if (!voluntarioValido) {
      this.form.controls.voluntarioId.patchValue(voluntarioBase > 0 ? voluntarioBase : null, { emitEvent: false });
    }

    this.atualizarMinisteriosDisponiveis(voluntarioBase);
    this.atualizarEstadoControles();
  }

  private aplicarContextoFormulario(): void {
    if (!this.aberto || !this.cultos.length) {
      return;
    }

    if (this.escalaEditando) {
      const voluntarioId = this.existeVoluntario(this.escalaEditando.voluntarioId)
        ? this.escalaEditando.voluntarioId
        : null;

      this.form.reset({
        cultoId: this.escalaEditando.cultoId,
        etapaCultoId: this.escalaEditando.etapaCultoId,
        tipoVoluntario: this.escalaEditando.voluntarioId ? 'CADASTRADO' : 'AVULSO',
        voluntarioId,
        voluntarioAvulsoNome: this.escalaEditando.voluntarioAvulsoNome || '',
        voluntarioAvulsoTelefone: this.escalaEditando.voluntarioAvulsoTelefone || '',
        ministerioId: this.escalaEditando.ministerioId,
        funcao: this.escalaEditando.funcao,
        presencaStatusId: this.escalaEditando.presencaStatusId,
        observacoes: this.escalaEditando.observacoes || ''
      });

      this.aplicarModoVoluntario();
      this.atualizarMinisteriosDisponiveis(Number(voluntarioId) || 0);
      this.atualizarEstadoControles();
      this.carregarEtapasDoCulto(this.escalaEditando.cultoId, this.escalaEditando.etapaCultoId);
      return;
    }

    const cultoId = this.prefill?.cultoId && this.cultos.some((culto) => Number(culto.id) === Number(this.prefill?.cultoId))
      ? this.prefill.cultoId
      : (this.cultos[0]?.id ?? 0);

    const voluntarioId = this.existeVoluntario(this.prefill?.voluntarioId)
      ? Number(this.prefill?.voluntarioId)
      : null;

    this.form.reset({
      cultoId,
      etapaCultoId: this.prefill?.etapaCultoId ?? null,
      tipoVoluntario: 'CADASTRADO',
      voluntarioId,
      voluntarioAvulsoNome: '',
      voluntarioAvulsoTelefone: '',
      ministerioId: this.prefill?.ministerioId ?? null,
      funcao: (this.prefill?.funcao || '').trim(),
      presencaStatusId: 1,
      observacoes: this.prefill?.observacoes || ''
    });

    this.aplicarModoVoluntario();
    this.atualizarMinisteriosDisponiveis(Number(voluntarioId) || 0);
    this.atualizarEstadoControles();
    this.carregarEtapasDoCulto(cultoId, this.prefill?.etapaCultoId ?? null);
  }

  get modoVoluntarioAvulso(): boolean {
    return this.form.controls.tipoVoluntario.value === 'AVULSO';
  }

  private carregarEtapasDoCulto(cultoId: number, etapaPreferencial: number | null = null): void {
    if (!cultoId) {
      this.etapasCulto = [];
      this.erroEtapas = null;
      this.form.controls.etapaCultoId.patchValue(null, { emitEvent: false });
      return;
    }

    this.carregandoEtapas = true;
    this.erroEtapas = null;

    this.cronogramaService.listarPorCulto(cultoId).subscribe({
      next: (data) => {
        this.etapasCulto = (data || [])
          .slice()
          .sort((a, b) => Number(a.sequencia || 0) - Number(b.sequencia || 0));

        const atual = Number(this.form.controls.etapaCultoId.value) || 0;
        const preferencial = Number(etapaPreferencial) || 0;
        const alvo = atual > 0 ? atual : preferencial;
        const existe = alvo > 0 && this.etapasCulto.some((item) => Number(item.id) === alvo);

        this.form.controls.etapaCultoId.patchValue(existe ? alvo : null, { emitEvent: false });
        this.sugerirMinisterioDaEtapa(existe ? alvo : 0);
      },
      error: () => {
        this.etapasCulto = [];
        this.erroEtapas = 'Falha ao carregar etapas do cronograma.';
        this.form.controls.etapaCultoId.patchValue(null, { emitEvent: false });
      },
      complete: () => {
        this.carregandoEtapas = false;
      }
    });
  }

  private sugerirMinisterioDaEtapa(etapaCultoId: number): void {
    if (!etapaCultoId) {
      return;
    }

    const etapa = this.etapasCulto.find((item) => Number(item.id) === Number(etapaCultoId));
    const ministerioEtapa = Number(etapa?.ministerioResponsavelId) || 0;
    if (!ministerioEtapa) {
      return;
    }

    const ministerioValido = this.ministeriosDisponiveis.some((item) => Number(item.id) === ministerioEtapa);
    if (!ministerioValido) {
      return;
    }

    const atual = Number(this.form.controls.ministerioId.value) || 0;
    if (!atual) {
      this.form.controls.ministerioId.patchValue(ministerioEtapa, { emitEvent: false });
    }

    this.atualizarFuncoesPorMinisterio(ministerioEtapa);
  }

  descricaoEtapa(etapa: EtapaCulto): string {
    const sequencia = Number(etapa.sequencia || 0);
    const atividade = String(etapa.atividade || '').trim() || 'Etapa';
    const bloco = String(etapa.blocoCronograma || 'PRINCIPAL').trim().toUpperCase();
    return `#${sequencia} - ${atividade} (${bloco})`;
  }

  get usarComboFuncao(): boolean {
    return this.funcoesPadraoDisponiveis.length > 0;
  }

  get funcaoAtualNaoCatalogada(): string | null {
    const atual = (this.form.controls.funcao.value || '').trim();
    if (!atual || !this.usarComboFuncao) {
      return null;
    }

    const existe = this.funcoesPadraoDisponiveis.some((item) => item.toLowerCase() === atual.toLowerCase());
    return existe ? null : atual;
  }

  get blocoCronogramaFuncaoSelecionada(): string {
    const funcao = (this.form.controls.funcao.value || '').trim();
    if (!funcao) {
      return 'Somente equipe';
    }

    const ministerioId = Number(this.form.controls.ministerioId.value) || 0;
    const ministerio = this.ministeriosDisponiveis.find((item) => Number(item.id) === ministerioId)
      || this.ministerios.find((item) => Number(item.id) === ministerioId);

    const funcaoPadrao = (ministerio?.funcoesPadrao || [])
      .find((item) => String(item.nome || '').trim().toLowerCase() === funcao.toLowerCase());

    return this.rotuloBlocoCronograma(funcaoPadrao?.blocoCronograma);
  }

  private rotuloBlocoCronograma(valor?: string | null): string {
    switch (String(valor || 'SOMENTE_EQUIPE').trim().toUpperCase()) {
      case 'PRINCIPAL':
        return 'Cronograma principal';
      case 'LOUNGE':
        return 'Lounge';
      case 'ADICIONAL':
        return 'Adicional';
      default:
        return 'Somente equipe';
    }
  }

  private obterVoluntarioPreferencial(): number {
    if (this.existeVoluntario(this.escalaEditando?.voluntarioId)) {
      return Number(this.escalaEditando?.voluntarioId);
    }

    if (this.existeVoluntario(this.prefill?.voluntarioId)) {
      return Number(this.prefill?.voluntarioId);
    }

    return 0;
  }

  private existeVoluntario(voluntarioId: number | null | undefined): boolean {
    return Number(voluntarioId) > 0
      && this.voluntarios.some((voluntario) => Number(voluntario.id) === Number(voluntarioId));
  }

  private atualizarMinisteriosDisponiveis(voluntarioId: number): void {
    if (this.modoVoluntarioAvulso) {
      this.ministeriosDisponiveis = (this.ministerios || [])
        .slice()
        .sort((a, b) => String(a.nome || '').localeCompare(String(b.nome || ''), 'pt-BR', { sensitivity: 'base' }));
      return;
    }

    const voluntario = this.voluntarios.find((item) => Number(item.id) === Number(voluntarioId));
    if (!voluntario) {
      this.ministeriosDisponiveis = [];
      this.funcoesPadraoDisponiveis = [];
      if ((Number(this.form.controls.ministerioId.value) || 0) > 0) {
        this.form.controls.ministerioId.patchValue(null, { emitEvent: false });
      }
      return;
    }

    const idsPermitidos = Array.from(new Set((voluntario.ministerioIds || [])
      .map((id) => Number(id))
      .filter((id) => id > 0)));

    if (!idsPermitidos.length && Number(voluntario.ministerioPrincipalId) > 0) {
      idsPermitidos.push(Number(voluntario.ministerioPrincipalId));
    }

    this.ministeriosDisponiveis = idsPermitidos.length
      ? this.ministerios.filter((ministerio) => idsPermitidos.includes(Number(ministerio.id)))
      : [];

    const ministerioSelecionado = Number(this.form.controls.ministerioId.value) || 0;
    if (ministerioSelecionado > 0 && !idsPermitidos.includes(ministerioSelecionado)) {
      this.form.controls.ministerioId.patchValue(null, { emitEvent: false });
    }

    this.atualizarFuncoesPorMinisterio(Number(this.form.controls.ministerioId.value) || 0);
  }

  private aplicarModoVoluntario(): void {
    if (this.modoVoluntarioAvulso) {
      this.cadastroVoluntarioAberto = false;
      this.form.controls.voluntarioId.patchValue(null, { emitEvent: false });
      this.ministeriosDisponiveis = (this.ministerios || [])
        .slice()
        .sort((a, b) => String(a.nome || '').localeCompare(String(b.nome || ''), 'pt-BR', { sensitivity: 'base' }));

      const ministerioControl = this.form.controls.ministerioId;
      if (ministerioControl.disabled) {
        ministerioControl.enable({ emitEvent: false });
      }
      this.atualizarFuncoesPorMinisterio(Number(ministerioControl.value) || 0);
      return;
    }

    this.form.controls.voluntarioAvulsoNome.patchValue('', { emitEvent: false });
    this.form.controls.voluntarioAvulsoTelefone.patchValue('', { emitEvent: false });
    this.atualizarMinisteriosDisponiveis(Number(this.form.controls.voluntarioId.value) || 0);
    this.atualizarEstadoControles();
  }

  private atualizarFuncoesPorMinisterio(ministerioId: number): void {
    if (!ministerioId) {
      this.funcoesPadraoDisponiveis = [];
      return;
    }

    const ministerio = this.ministeriosDisponiveis.find((item) => Number(item.id) === Number(ministerioId));
    const funcoes = (ministerio?.funcoesPadrao || [])
      .filter((item) => item?.ativo !== false && !!String(item.nome || '').trim())
      .slice()
      .sort((a, b) => {
        const ordemA = Number(a.ordem || 0);
        const ordemB = Number(b.ordem || 0);
        if (ordemA !== ordemB) {
          return ordemA - ordemB;
        }
        return String(a.nome || '').localeCompare(String(b.nome || ''), 'pt-BR', { sensitivity: 'base' });
      })
      .map((item) => String(item.nome || '').trim());

    this.funcoesPadraoDisponiveis = Array.from(new Set(funcoes));

    if (!this.funcoesPadraoDisponiveis.length) {
      return;
    }

    const atual = (this.form.controls.funcao.value || '').trim();
    if (!atual) {
      this.form.controls.funcao.patchValue(this.funcoesPadraoDisponiveis[0], { emitEvent: false });
    }
  }

  private atualizarEstadoControles(): void {
    const voluntarioControl = this.form.controls.voluntarioId;
    const ministerioControl = this.form.controls.ministerioId;

    if (this.modoVoluntarioAvulso) {
      if (ministerioControl.disabled) {
        ministerioControl.enable({ emitEvent: false });
      }
      return;
    }

    const voluntarioSelecionado = Number(voluntarioControl.value) || 0;
    if (!this.existeVoluntario(voluntarioSelecionado)) {
      if (ministerioControl.enabled) {
        ministerioControl.disable({ emitEvent: false });
      }
      if (ministerioControl.value !== null) {
        ministerioControl.patchValue(null, { emitEvent: false });
      }
      return;
    }

    if (ministerioControl.disabled) {
      ministerioControl.enable({ emitEvent: false });
    }
  }

  private garantirVoluntariosDisponiveis(forcar = false): void {
    if (!this.aberto || this.voluntarios.length || this.carregandoVoluntariosFallback) {
      return;
    }

    if (!forcar && this.tentouCarregarVoluntariosFallback) {
      return;
    }

    this.tentouCarregarVoluntariosFallback = true;
    this.carregandoVoluntariosFallback = true;
    this.erroVoluntariosFallback = null;
    this.atualizarEstadoControles();

    this.cadastroService.listarVoluntarios().subscribe({
      next: (data) => {
        this.voluntarios = (data || [])
          .slice()
          .sort((a, b) => (a.nome || '').localeCompare(b.nome || '', 'pt-BR', { sensitivity: 'base' }));

        if (!this.voluntarios.length && this.erroVoluntarios) {
          this.erroVoluntariosFallback = this.erroVoluntarios;
        } else {
          this.erroVoluntariosFallback = null;
        }

        this.sincronizarSelecaoComDados();
      },
      error: () => {
        if (!this.voluntarios.length) {
          this.erroVoluntariosFallback = 'Falha ao carregar voluntários.';
        }
      },
      complete: () => {
        this.carregandoVoluntariosFallback = false;
        this.atualizarEstadoControles();
      }
    });
  }

  get carregandoVoluntariosEfetivo(): boolean {
    return this.carregandoVoluntarios || this.carregandoVoluntariosFallback;
  }

  get erroVoluntariosEfetivo(): string | null {
    return this.erroVoluntarios || this.erroVoluntariosFallback;
  }
}
