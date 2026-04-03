import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { MinisterioCompleto, MinisterioRequest } from '../../core/models/ministerio.models';
import { MinisterioService } from '../../core/services/ministerio.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

@Component({
  selector: 'app-ministerios',
  templateUrl: './ministerios.component.html',
  styleUrls: ['./ministerios.component.scss']
})
export class MinisteriosComponent implements OnInit {
  ministerios: MinisterioCompleto[] = [];
  usuarios: { id: number; nome: string; email: string }[] = [];
  voluntarios: { id: number; nome: string; email: string }[] = [];
  modalAberto = false;
  ministerioEditandoId: number | null = null;
  carregando = false;
  termoBusca = '';

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    descricao: [''],
    ativo: [true],
    lideresIds: [[] as number[]],
    liderPrincipalId: [null as number | null],
    voluntarioIds: [[] as number[]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly ministerioService: MinisterioService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.carregarOpcoes();
    this.carregar();
  }

  carregar(): void {
    this.carregando = true;
    this.ministerioService.listar(this.termoBusca || undefined).subscribe({
      next: (data) => {
        this.ministerios = data;
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  filtrar(): void {
    this.carregar();
  }

  abrirModalNovo(): void {
    this.limparFormulario();
    this.modalAberto = true;
  }

  editar(ministerio: MinisterioCompleto): void {
    this.ministerioEditandoId = ministerio.id;
    this.modalAberto = true;
    const lideresIds = ministerio.lideres.map((x) => x.usuarioId);
    const liderPrincipalId = ministerio.lideres.find((x) => x.principal)?.usuarioId ?? null;
    const voluntarioIds = ministerio.voluntarios.map((x) => x.voluntarioId);

    this.form.patchValue({
      nome: ministerio.nome,
      descricao: ministerio.descricao || '',
      ativo: ministerio.ativo,
      lideresIds,
      liderPrincipalId,
      voluntarioIds
    });
  }

  cancelarEdicao(): void {
    this.limparFormulario();
    this.modalAberto = false;
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const lideresIds = (raw.lideresIds || []).filter((x) => x > 0);
    const liderPrincipalId = raw.liderPrincipalId;
    const payload: MinisterioRequest = {
      nome: raw.nome || '',
      descricao: raw.descricao || null,
      ativo: raw.ativo ?? true,
      lideres: lideresIds.map((id) => ({ usuarioId: id, principal: id === liderPrincipalId })),
      voluntarioIds: (raw.voluntarioIds || []).filter((x) => x > 0)
    };

    const requisicao = this.ministerioEditandoId === null
      ? this.ministerioService.criar(payload)
      : this.ministerioService.atualizar(this.ministerioEditandoId, payload);

    requisicao.subscribe({
      next: () => {
        this.toastr.success(this.ministerioEditandoId === null ? 'Ministério criado.' : 'Ministério atualizado.', 'Ministérios');
        this.cancelarEdicao();
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar ministério.', 'Erro');
      }
    });
  }

  async excluir(ministerio: MinisterioCompleto): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir o ministério "${ministerio.nome}"?`);
    if (!confirmou) {
      return;
    }

    this.ministerioService.excluir(ministerio.id).subscribe({
      next: () => {
        this.toastr.success('Ministério excluído com sucesso.', 'Ministérios');
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível excluir ministério.', 'Erro');
      }
    });
  }

  private carregarOpcoes(): void {
    this.ministerioService.opcoes().subscribe((data) => {
      this.usuarios = data.usuarios;
      this.voluntarios = data.voluntarios;
    });
  }

  private limparFormulario(): void {
    this.ministerioEditandoId = null;
    this.form.reset({
      nome: '',
      descricao: '',
      ativo: true,
      lideresIds: [],
      liderPrincipalId: null,
      voluntarioIds: []
    });
  }
}
