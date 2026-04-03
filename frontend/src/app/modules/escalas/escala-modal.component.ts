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
  voluntarioId: number;
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
  @Input() ministerios: Ministerio[] = [];
  @Input() escalaEditando: Escala | null = null;
  @Input() prefill: EscalaModalPrefill | null = null;

  @Output() fechar = new EventEmitter<void>();
  @Output() salvou = new EventEmitter<void>();

  ministeriosDisponiveis: Ministerio[] = [];
  cadastroVoluntarioAberto = false;
  salvando = false;

  readonly form = this.fb.group({
    cultoId: [0, Validators.required],
    etapaCultoId: [null as number | null],
    voluntarioId: [0, Validators.required],
    ministerioId: [null as number | null],
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
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    const abriuModal = !!changes['aberto']?.currentValue && !changes['aberto']?.previousValue;
    const alterouDados = !!changes['cultos'] || !!changes['voluntarios'] || !!changes['ministerios'];

    if (abriuModal || (this.aberto && alterouDados)) {
      this.aplicarEstadoInicial();
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
    const ministerioIds = Array.from(new Set((raw.ministerioIds || []).filter((id) => Number(id) > 0)));

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
        this.voluntarios = [...this.voluntarios, novo].sort((a, b) => a.nome.localeCompare(b.nome));
        this.form.patchValue({ voluntarioId: novo.id });
        this.atualizarMinisteriosDisponiveis(novo.id);
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível cadastrar voluntário.', 'Erro');
      }
    });
  }

  fecharModal(): void {
    this.fechar.emit();
  }

  private aplicarEstadoInicial(): void {
    if (!this.aberto || !this.cultos.length || !this.voluntarios.length) {
      return;
    }

    this.cadastroVoluntarioAberto = false;

    if (this.escalaEditando) {
      this.form.reset({
        cultoId: this.escalaEditando.cultoId,
        etapaCultoId: this.escalaEditando.etapaCultoId,
        voluntarioId: this.escalaEditando.voluntarioId,
        ministerioId: this.escalaEditando.ministerioId,
        funcao: this.escalaEditando.funcao,
        presencaStatusId: this.escalaEditando.presencaStatusId,
        observacoes: this.escalaEditando.observacoes || ''
      });
      this.atualizarMinisteriosDisponiveis(this.escalaEditando.voluntarioId);
      return;
    }

    const cultoId = this.prefill?.cultoId && this.cultos.some((c) => c.id === this.prefill?.cultoId)
      ? this.prefill.cultoId
      : (this.cultos[0]?.id ?? 0);

    const voluntarioId = this.prefill?.voluntarioId && this.voluntarios.some((v) => v.id === this.prefill?.voluntarioId)
      ? this.prefill.voluntarioId
      : (this.voluntarios[0]?.id ?? 0);

    this.atualizarMinisteriosDisponiveis(voluntarioId);
    const ministerioIdValido = (this.prefill?.ministerioId ?? null) && this.ministeriosDisponiveis.some((m) => m.id === this.prefill?.ministerioId)
      ? this.prefill?.ministerioId ?? null
      : null;

    this.form.reset({
      cultoId,
      etapaCultoId: null,
      voluntarioId,
      ministerioId: ministerioIdValido,
      funcao: (this.prefill?.funcao || '').trim(),
      presencaStatusId: 1,
      observacoes: this.prefill?.observacoes || ''
    });
  }

  private atualizarMinisteriosDisponiveis(voluntarioId: number): void {
    const voluntario = this.voluntarios.find((item) => item.id === voluntarioId);
    const idsPermitidos = voluntario?.ministerioIds ?? [];
    this.ministeriosDisponiveis = this.ministerios.filter((ministerio) => idsPermitidos.includes(ministerio.id));

    const ministerioSelecionado = Number(this.form.value.ministerioId) || 0;
    if (ministerioSelecionado > 0 && !idsPermitidos.includes(ministerioSelecionado)) {
      this.form.patchValue({ ministerioId: null });
    }
  }
}
