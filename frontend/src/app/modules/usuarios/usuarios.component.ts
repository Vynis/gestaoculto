import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { CellClickedEvent, ColDef } from 'ag-grid-community';
import { PerfilOpcao, UsuarioRequest, Usuario } from '../../core/models/usuario.models';
import { UsuarioService } from '../../core/services/usuario.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

interface UsuarioGridRow {
  nome: string;
  email: string;
  telefone: string;
  ativo: string;
  perfis: string;
  usuario: Usuario;
}

@Component({
  selector: 'app-usuarios',
  templateUrl: './usuarios.component.html',
  styleUrls: ['./usuarios.component.scss']
})
export class UsuariosComponent implements OnInit {
  usuarios: Usuario[] = [];
  rowData: UsuarioGridRow[] = [];
  perfis: PerfilOpcao[] = [];
  modalAberto = false;
  usuarioEditandoId: number | null = null;
  carregando = false;
  termoBusca = '';

  readonly defaultColDef: ColDef<UsuarioGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 120
  };

  readonly columnDefs: ColDef<UsuarioGridRow>[] = [
    { headerName: 'Nome', field: 'nome', minWidth: 180 },
    { headerName: 'E-mail', field: 'email', minWidth: 220 },
    { headerName: 'Telefone', field: 'telefone', minWidth: 140 },
    { headerName: 'Ativo', field: 'ativo', minWidth: 110, maxWidth: 130 },
    { headerName: 'Perfis', field: 'perfis', minWidth: 220 },
    {
      headerName: 'Acoes',
      field: 'usuario',
      minWidth: 110,
      maxWidth: 130,
      sortable: false,
      filter: false,
      cellRenderer: () =>
        '<div class="grid-actions"><button class="grid-btn icon edit" data-action="editar" type="button" title="Editar usuario" aria-label="Editar usuario"><span class="icon-pencil"></span></button><button class="grid-btn icon delete" data-action="excluir" type="button" title="Excluir usuario" aria-label="Excluir usuario"><span class="icon-trash"></span></button></div>'
    }
  ];

  readonly paginationPageSize = 8;

  readonly localeText = {
    noRowsToShow: 'Nenhum usuario cadastrado.'
  };

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    telefone: [''],
    ativo: [true],
    senha: ['', Validators.required],
    perfilIds: [[] as number[]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly usuarioService: UsuarioService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.carregarOpcoes();
    this.carregar();
  }

  carregar(): void {
    this.carregando = true;
    this.usuarioService.listar(this.termoBusca || undefined).subscribe({
      next: (data) => {
        this.usuarios = data;
        this.rowData = this.mapearParaGrid(data);
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  filtrar(): void {
    this.carregar();
  }

  onGridCellClicked(event: CellClickedEvent<UsuarioGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data?.usuario) {
      return;
    }

    if (action === 'editar') {
      this.editar(event.data.usuario);
      return;
    }

    if (action === 'excluir') {
      this.excluir(event.data.usuario);
    }
  }

  abrirModalNovo(): void {
    this.limparFormulario();
    this.configurarValidacaoSenha(true);
    this.modalAberto = true;
  }

  editar(usuario: Usuario): void {
    this.usuarioEditandoId = usuario.id;
    this.configurarValidacaoSenha(false);
    this.modalAberto = true;
    this.form.patchValue({
      nome: usuario.nome,
      email: usuario.email,
      telefone: usuario.telefone || '',
      ativo: usuario.ativo,
      senha: '',
      perfilIds: usuario.perfilIds
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
    const senha = (raw.senha || '').trim();
    const payload: UsuarioRequest = {
      nome: (raw.nome || '').trim(),
      email: (raw.email || '').trim(),
      telefone: (raw.telefone || '').trim() || null,
      ativo: raw.ativo ?? true,
      senha: senha.length ? senha : null,
      perfilIds: (raw.perfilIds || []).filter((x) => x > 0)
    };

    const requisicao = this.usuarioEditandoId === null
      ? this.usuarioService.criar(payload)
      : this.usuarioService.atualizar(this.usuarioEditandoId, payload);

    requisicao.subscribe({
      next: () => {
        this.toastr.success(this.usuarioEditandoId === null ? 'Usuário criado.' : 'Usuário atualizado.', 'Usuários');
        this.cancelarEdicao();
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar usuário.', 'Erro');
      }
    });
  }

  async excluir(usuario: Usuario): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir o usuário "${usuario.nome}"?`);
    if (!confirmou) {
      return;
    }

    this.usuarioService.excluir(usuario.id).subscribe({
      next: () => {
        this.toastr.success('Usuário excluído com sucesso.', 'Usuários');
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível excluir usuário.', 'Erro');
      }
    });
  }

  private carregarOpcoes(): void {
    this.usuarioService.opcoes().subscribe((data) => {
      this.perfis = data.perfis;
    });
  }

  private limparFormulario(): void {
    this.usuarioEditandoId = null;
    this.form.reset({
      nome: '',
      email: '',
      telefone: '',
      ativo: true,
      senha: '',
      perfilIds: []
    });
    this.configurarValidacaoSenha(true);
  }

  private configurarValidacaoSenha(obrigatoria: boolean): void {
    if (obrigatoria) {
      this.form.controls.senha.setValidators([Validators.required]);
    } else {
      this.form.controls.senha.clearValidators();
    }

    this.form.controls.senha.updateValueAndValidity();
  }

  private mapearParaGrid(lista: Usuario[]): UsuarioGridRow[] {
    return lista.map((usuario) => ({
      nome: usuario.nome,
      email: usuario.email,
      telefone: usuario.telefone || 'Sem telefone',
      ativo: usuario.ativo ? 'Sim' : 'Nao',
      perfis: usuario.perfis.length ? usuario.perfis.join(', ') : 'Sem perfil',
      usuario
    }));
  }
}
