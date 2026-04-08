import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { Culto } from '../../core/models/culto.models';
import { Ministerio, Voluntario } from '../../core/models/cadastro.models';
import { Escala } from '../../core/models/escala.models';
import { EscalaService } from '../../core/services/escala.service';
import { CadastroService } from '../../core/services/cadastro.service';

export interface EscalaModalPrefill {
  cultoId: number;
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
  cadastroVoluntarioAberto = false;
  salvando = false;
  carregandoVoluntariosFallback = false;
  erroVoluntariosFallback: string | null = null;

  private tentouCarregarVoluntariosFallback = false;

  readonly form = this.fb.group({
    cultoId: [0, Validators.required],
    etapaCultoId: [null as number | null],
    voluntarioId: [null as number | null, Validators.required],
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
    private readonly toastr: NbToastrService
  ) {
    this.form.controls.voluntarioId.valueChanges.subscribe((voluntarioId) => {
      this.atualizarMinisteriosDisponiveis(Number(voluntarioId) || 0);
      this.atualizarEstadoControles();
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
    const payload = {
      cultoId: raw.cultoId ?? 0,
      etapaCultoId: raw.etapaCultoId ?? null,
      voluntarioId: raw.voluntarioId ?? 0,
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
        voluntarioId,
        ministerioId: this.escalaEditando.ministerioId,
        funcao: this.escalaEditando.funcao,
        presencaStatusId: this.escalaEditando.presencaStatusId,
        observacoes: this.escalaEditando.observacoes || ''
      });

      this.atualizarMinisteriosDisponiveis(Number(voluntarioId) || 0);
      this.atualizarEstadoControles();
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
      etapaCultoId: null,
      voluntarioId,
      ministerioId: this.prefill?.ministerioId ?? null,
      funcao: (this.prefill?.funcao || '').trim(),
      presencaStatusId: 1,
      observacoes: this.prefill?.observacoes || ''
    });

    this.atualizarMinisteriosDisponiveis(Number(voluntarioId) || 0);
    this.atualizarEstadoControles();
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
    const voluntario = this.voluntarios.find((item) => Number(item.id) === Number(voluntarioId));
    if (!voluntario) {
      this.ministeriosDisponiveis = [];
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
  }

  private atualizarEstadoControles(): void {
    const voluntarioControl = this.form.controls.voluntarioId;
    const ministerioControl = this.form.controls.ministerioId;

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
