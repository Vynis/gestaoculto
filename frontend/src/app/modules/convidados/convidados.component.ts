import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Convidado } from '../../core/models/convidado.models';
import { ConvidadoService } from '../../core/services/convidado.service';
import { CultoService } from '../../core/services/culto.service';
import { Culto } from '../../core/models/culto.models';
import { NbToastrService } from '@nebular/theme';

@Component({
  selector: 'app-convidados',
  templateUrl: './convidados.component.html',
  styleUrls: ['./convidados.component.scss']
})
export class ConvidadosComponent implements OnInit {
  convidados: Convidado[] = [];
  cultos: Culto[] = [];
  modalAberto = false;
  convidadoEditandoId: number | null = null;

  readonly filtro = this.fb.group({
    cultoId: [0, Validators.required]
  });

  readonly form = this.fb.group({
    cultoId: [0, Validators.required],
    nome: ['', Validators.required],
    telefone: [''],
    quemConvidou: [''],
    primeiraVezIgreja: [true],
    observacoes: [''],
    statusAcompanhamento: ['PENDENTE']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly convidadoService: ConvidadoService,
    private readonly cultoService: CultoService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.cultoService.listarAtivos().subscribe((data) => {
      this.cultos = data;
      const primeiro = data[0]?.id ?? 0;
      this.filtro.patchValue({ cultoId: primeiro });
      this.form.patchValue({ cultoId: primeiro });
      if (primeiro) {
        this.carregar();
      }
    });
  }

  carregar(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      return;
    }

    this.convidadoService.listar(cultoId).subscribe((data) => {
      this.convidados = data;
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
      nome: raw.nome ?? '',
      telefone: raw.telefone ?? null,
      quemConvidou: raw.quemConvidou ?? null,
      primeiraVezIgreja: raw.primeiraVezIgreja ?? true,
      observacoes: raw.observacoes ?? null,
      statusAcompanhamento: raw.statusAcompanhamento ?? 'PENDENTE'
    };

    const requisicao = this.convidadoEditandoId === null
      ? this.convidadoService.criar(payload)
      : this.convidadoService.atualizar(this.convidadoEditandoId, payload);

    requisicao.subscribe(() => {
      this.filtro.patchValue({ cultoId: Number(this.form.value.cultoId) });
      this.carregar();
      this.toastr.success(
        this.convidadoEditandoId === null
          ? 'Convidado registrado com sucesso.'
          : 'Convidado atualizado com sucesso.',
        'Cadastro rápido'
      );
      this.limparFormulario();
      this.fecharModal();
    });
  }

  abrirModalNovo(): void {
    this.limparFormulario();
    this.modalAberto = true;
  }

  editar(convidado: Convidado): void {
    this.convidadoEditandoId = convidado.id;
    this.modalAberto = true;
    this.form.patchValue({
      cultoId: convidado.cultoId,
      nome: convidado.nome,
      telefone: convidado.telefone || '',
      quemConvidou: convidado.quemConvidou || '',
      primeiraVezIgreja: convidado.primeiraVezIgreja,
      observacoes: convidado.observacoes || '',
      statusAcompanhamento: convidado.statusAcompanhamento
    });
  }

  cancelarEdicao(): void {
    this.limparFormulario();
    this.fecharModal();
  }

  fecharModal(): void {
    this.modalAberto = false;
  }

  private limparFormulario(): void {
    this.convidadoEditandoId = null;
    const cultoIdAtual = Number(this.filtro.value.cultoId) || Number(this.form.value.cultoId) || 0;
    this.form.reset({
      cultoId: cultoIdAtual,
      nome: '',
      telefone: '',
      quemConvidou: '',
      primeiraVezIgreja: true,
      observacoes: '',
      statusAcompanhamento: 'PENDENTE'
    });
  }
}
