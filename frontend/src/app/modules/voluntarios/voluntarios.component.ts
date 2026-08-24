import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { CellClickedEvent, ColDef } from 'ag-grid-community';
import { Ministerio, UsuarioOpcaoVoluntario, Voluntario } from '../../core/models/cadastro.models';
import { CadastroService } from '../../core/services/cadastro.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

interface VoluntarioGridRow {
  nome: string;
  usuario: string;
  ministerios: string;
  email: string;
  ativo: string;
  voluntario: Voluntario;
}

@Component({
  selector: 'app-voluntarios',
  templateUrl: './voluntarios.component.html',
  styleUrls: ['./voluntarios.component.scss']
})
export class VoluntariosComponent implements OnInit {
  voluntarios: Voluntario[] = [];
  rowData: VoluntarioGridRow[] = [];
  filtroGrid = '';
  ministerios: Ministerio[] = [];
  usuarios: UsuarioOpcaoVoluntario[] = [];
  carregando = false;
  modalAberto = false;
  voluntarioEditandoId: number | null = null;

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    usuarioId: [null as number | null],
    telefone: [''],
    email: [''],
    ministerioIds: [[] as number[]],
    observacoes: [''],
    restricoesIndisponibilidade: [''],
    ativo: [true]
  });

  readonly defaultColDef: ColDef<VoluntarioGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 120
  };

  readonly columnDefs: ColDef<VoluntarioGridRow>[] = [
    { headerName: 'Nome', field: 'nome', minWidth: 180 },
    { headerName: 'Usuario vinculado', field: 'usuario', minWidth: 190 },
    { headerName: 'Ministérios', field: 'ministerios', minWidth: 220 },
    { headerName: 'E-mail', field: 'email', minWidth: 180 },
    { headerName: 'Status', field: 'ativo', minWidth: 120, maxWidth: 140 },
    {
      headerName: 'Ações',
      field: 'voluntario',
      minWidth: 140,
      maxWidth: 170,
      sortable: false,
      filter: false,
      cellRenderer: () =>
        '<div class="grid-actions"><button class="grid-btn icon edit" data-action="editar" type="button" title="Editar voluntário" aria-label="Editar voluntário"><span class="icon-pencil"></span></button><button class="grid-btn icon delete" data-action="excluir" type="button" title="Excluir voluntário" aria-label="Excluir voluntário"><span class="icon-trash"></span></button></div>'
    }
  ];

  readonly paginationPageSize = 8;

  readonly localeText = {
    noRowsToShow: 'Nenhum voluntário cadastrado.'
  };

  constructor(
    private readonly fb: FormBuilder,
    private readonly cadastroService: CadastroService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.carregarUsuarios();

    this.cadastroService.listarMinisterios().subscribe((data) => {
      this.ministerios = data;
      this.rowData = this.mapearParaGrid(this.voluntarios);
    });

    this.carregar();
  }

  carregar(): void {
    this.carregando = true;
    this.cadastroService.listarVoluntarios().subscribe({
      next: (data) => {
        this.voluntarios = data;
        this.rowData = this.mapearParaGrid(data);
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

    const raw = this.form.getRawValue();
    const nome = (raw.nome || '').trim();
    const email = (raw.email || '').trim().toLowerCase();

    const nomeDuplicado = this.voluntarios.some((v) =>
      v.id !== (this.voluntarioEditandoId ?? 0)
      && v.nome.trim().toLowerCase() === nome.toLowerCase()
    );
    if (nomeDuplicado) {
      this.toastr.warning('Já existe voluntário cadastrado com este nome.', 'Cadastro rápido');
      return;
    }

    if (email) {
      const emailDuplicado = this.voluntarios.some((v) =>
        v.id !== (this.voluntarioEditandoId ?? 0)
        && (v.email || '').trim().toLowerCase() === email
      );
      if (emailDuplicado) {
        this.toastr.warning('Já existe voluntário cadastrado com este e-mail.', 'Cadastro rápido');
        return;
      }
    }

    const ministerioIds = (raw.ministerioIds || []).filter((id) => id > 0);
    const payload: Voluntario = {
      id: this.voluntarioEditandoId ?? 0,
      nome,
      usuarioId: raw.usuarioId ?? null,
      telefone: raw.telefone || null,
      email: email || null,
      ministerioPrincipalId: ministerioIds[0] ?? null,
      ministerioIds,
      observacoes: raw.observacoes || null,
      restricoesIndisponibilidade: raw.restricoesIndisponibilidade || null,
      ativo: raw.ativo ?? true
    };

    const requisicao = this.voluntarioEditandoId === null
      ? this.cadastroService.criarVoluntario(payload)
      : this.cadastroService.atualizarVoluntario(this.voluntarioEditandoId, payload);

    requisicao.subscribe({
      next: () => {
        this.toastr.success(
          this.voluntarioEditandoId === null
            ? 'Voluntário cadastrado com sucesso.'
            : 'Voluntário atualizado com sucesso.',
          'Cadastro rápido'
        );
        this.limparFormulario();
        this.fecharModal();
        this.carregar();
        this.carregarUsuarios();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar voluntário.', 'Erro');
      }
    });
  }

  abrirModalNovo(): void {
    this.limparFormulario();
    this.modalAberto = true;
  }

  fecharModal(): void {
    this.modalAberto = false;
  }

  editar(voluntario: Voluntario): void {
    this.modalAberto = true;
    this.voluntarioEditandoId = voluntario.id;
    this.form.patchValue({
      nome: voluntario.nome,
      usuarioId: voluntario.usuarioId ?? null,
      telefone: voluntario.telefone || '',
      email: voluntario.email || '',
      ministerioIds: voluntario.ministerioIds || [],
      observacoes: voluntario.observacoes || '',
      restricoesIndisponibilidade: voluntario.restricoesIndisponibilidade || '',
      ativo: voluntario.ativo
    });
  }

  onGridCellClicked(event: CellClickedEvent<VoluntarioGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data?.voluntario) {
      return;
    }

    if (action === 'editar') {
      this.editar(event.data.voluntario);
      return;
    }

    if (action === 'excluir') {
      this.excluir(event.data.voluntario);
    }
  }

  cancelarEdicao(): void {
    this.limparFormulario();
    this.fecharModal();
  }

  ministeriosNomes(ids: number[] | null | undefined): string {
    if (!ids || ids.length === 0) {
      return 'Sem ministério';
    }

    const nomes = ids
      .map((id) => this.ministerios.find((m) => m.id === id)?.nome)
      .filter((nome): nome is string => Boolean(nome));

    return nomes.length > 0 ? nomes.join(', ') : 'Sem ministério';
  }

  usuariosDisponiveis(): UsuarioOpcaoVoluntario[] {
    const voluntarioAtualId = this.voluntarioEditandoId;
    return this.usuarios.filter((usuario) => !usuario.voluntarioId || usuario.voluntarioId === voluntarioAtualId);
  }

  private limparFormulario(): void {
    this.voluntarioEditandoId = null;
    this.form.reset({
      nome: '',
      usuarioId: null,
      telefone: '',
      email: '',
      ministerioIds: [],
      observacoes: '',
      restricoesIndisponibilidade: '',
      ativo: true
    });
  }

  private mapearParaGrid(voluntarios: Voluntario[]): VoluntarioGridRow[] {
    return voluntarios.map((voluntario) => ({
      nome: voluntario.nome,
      usuario: voluntario.usuarioNome || 'Nao vinculado',
      ministerios: this.ministeriosNomes(voluntario.ministerioIds),
      email: voluntario.email || 'Sem e-mail',
      ativo: voluntario.ativo ? 'Ativo' : 'Inativo',
      voluntario
    }));
  }

  private carregarUsuarios(): void {
    this.cadastroService.listarUsuariosParaVoluntario().subscribe((data) => {
      this.usuarios = data.usuarios || [];
    });
  }

  private async excluir(voluntario: Voluntario): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja realmente excluir o voluntário ${voluntario.nome}?`);
    if (!confirmou) {
      return;
    }

    this.cadastroService.excluirVoluntario(voluntario.id).subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem || 'Voluntário excluído com sucesso.', 'Cadastro rápido');
        this.carregar();
        this.carregarUsuarios();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível excluir voluntário.', 'Erro');
      }
    });
  }
}
