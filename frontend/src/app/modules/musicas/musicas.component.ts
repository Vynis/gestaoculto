import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { CellClickedEvent, ColDef, ICellRendererParams } from 'ag-grid-community';
import { NbToastrService } from '@nebular/theme';
import { Musica, MusicaRequest } from '../../core/models/musica.models';
import { MusicaService } from '../../core/services/musica.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';
import { TONS_MUSICA } from '../../core/models/tom.models';

interface MusicaGridRow {
  titulo: string;
  artistaBanda: string;
  tom: string;
  status: string;
  linkCifra: string;
  linkVideo: string;
  musica: Musica;
}

@Component({
  selector: 'app-musicas',
  templateUrl: './musicas.component.html',
  styleUrls: ['./musicas.component.scss']
})
export class MusicasComponent implements OnInit {
  musicas: Musica[] = [];
  rowData: MusicaGridRow[] = [];
  modalAberto = false;
  musicaEditandoId: number | null = null;
  filtroGrid = '';
  filtroAtivo: '' | 'true' | 'false' = '';
  readonly tonsMusica = TONS_MUSICA;

  readonly defaultColDef: ColDef<MusicaGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 120
  };

  readonly columnDefs: ColDef<MusicaGridRow>[] = [
    { headerName: 'Título', field: 'titulo', minWidth: 190 },
    { headerName: 'Artista/Banda', field: 'artistaBanda', minWidth: 180 },
    { headerName: 'Tom', field: 'tom', minWidth: 90, maxWidth: 110 },
    { headerName: 'Status', field: 'status', minWidth: 110, maxWidth: 130 },
    {
      headerName: 'Cifra',
      field: 'linkCifra',
      minWidth: 100,
      maxWidth: 120,
      cellRenderer: (params: ICellRendererParams<MusicaGridRow>) => this.renderizarLink(params.value, 'Abrir')
    },
    {
      headerName: 'Vídeo',
      field: 'linkVideo',
      minWidth: 100,
      maxWidth: 120,
      cellRenderer: (params: ICellRendererParams<MusicaGridRow>) => this.renderizarLink(params.value, 'Abrir')
    },
    {
      headerName: 'Ações',
      field: 'musica',
      minWidth: 130,
      maxWidth: 160,
      sortable: false,
      filter: false,
      cellRenderer: () =>
        '<div class="grid-actions"><button class="grid-btn icon edit" data-action="editar" type="button" title="Editar música" aria-label="Editar música"><span class="icon-pencil"></span></button><button class="grid-btn icon delete" data-action="excluir" type="button" title="Excluir música" aria-label="Excluir música"><span class="icon-trash"></span></button></div>'
    }
  ];

  readonly paginationPageSize = 8;

  readonly localeText = {
    noRowsToShow: 'Nenhuma música cadastrada.'
  };

  readonly form = this.fb.group({
    titulo: ['', Validators.required],
    artistaBanda: ['', Validators.required],
    tom: [''],
    linkCifra: [''],
    linkVideo: [''],
    observacoes: [''],
    ativo: [true]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly musicaService: MusicaService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    const ativo = this.filtroAtivo === '' ? undefined : this.filtroAtivo === 'true';
    this.musicaService.listar(undefined, ativo).subscribe((data) => {
      this.musicas = data;
      this.rowData = this.mapearParaGrid(data);
    });
  }

  onGridCellClicked(event: CellClickedEvent<MusicaGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data?.musica) {
      return;
    }

    if (action === 'editar') {
      this.editar(event.data.musica);
      return;
    }

    if (action === 'excluir') {
      this.excluir(event.data.musica);
    }
  }

  abrirModalNova(): void {
    this.limparFormulario();
    this.modalAberto = true;
  }

  editar(musica: Musica): void {
    this.musicaEditandoId = musica.id;
    this.modalAberto = true;
    this.form.patchValue({
      titulo: musica.titulo,
      artistaBanda: musica.artistaBanda,
      tom: musica.tom || '',
      linkCifra: musica.linkCifra || '',
      linkVideo: musica.linkVideo || '',
      observacoes: musica.observacoes || '',
      ativo: musica.ativo
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const titulo = (raw.titulo || '').trim();
    const artistaBanda = (raw.artistaBanda || '').trim();

    const duplicadaLocal = this.musicas.some((musica) =>
      musica.id !== this.musicaEditandoId
      && musica.titulo.trim().toLowerCase() === titulo.toLowerCase()
      && musica.artistaBanda.trim().toLowerCase() === artistaBanda.toLowerCase()
    );

    if (duplicadaLocal) {
      this.toastr.warning('Já existe música com mesmo título e artista/banda.', 'Louvor');
      return;
    }

    const payload: MusicaRequest = {
      titulo,
      artistaBanda,
      tom: raw.tom || null,
      linkCifra: raw.linkCifra || null,
      linkVideo: raw.linkVideo || null,
      observacoes: raw.observacoes || null,
      ativo: raw.ativo ?? true
    };

    const requisicao = this.musicaEditandoId === null
      ? this.musicaService.criar(payload)
      : this.musicaService.atualizar(this.musicaEditandoId, payload);

    requisicao.subscribe({
      next: () => {
        this.toastr.success(this.musicaEditandoId === null ? 'Música cadastrada.' : 'Música atualizada.', 'Louvor');
        this.modalAberto = false;
        this.limparFormulario();
        this.carregar();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar música.', 'Erro');
      }
    });
  }

  async excluir(musica: Musica): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir a música "${musica.titulo}"?`);
    if (!confirmou) {
      return;
    }

    this.musicaService.excluir(musica.id).subscribe(() => {
      this.toastr.success('Música excluída com sucesso.', 'Louvor');
      this.carregar();
    });
  }

  fecharModal(): void {
    this.modalAberto = false;
    this.limparFormulario();
  }

  private limparFormulario(): void {
    this.musicaEditandoId = null;
    this.form.reset({
      titulo: '',
      artistaBanda: '',
      tom: '',
      linkCifra: '',
      linkVideo: '',
      observacoes: '',
      ativo: true
    });
  }

  private mapearParaGrid(musicas: Musica[]): MusicaGridRow[] {
    return musicas.map((musica) => ({
      titulo: musica.titulo,
      artistaBanda: musica.artistaBanda,
      tom: musica.tom || '-',
      status: musica.ativo ? 'Ativa' : 'Inativa',
      linkCifra: musica.linkCifra || '',
      linkVideo: musica.linkVideo || '',
      musica
    }));
  }

  private renderizarLink(url: string | null | undefined, texto: string): string {
    const urlSegura = this.normalizarUrl(url);
    return urlSegura
      ? `<a class="grid-link" href="${this.escaparAtributo(urlSegura)}" target="_blank" rel="noopener noreferrer">${texto}</a>`
      : '-';
  }

  private normalizarUrl(url: string | null | undefined): string | null {
    const texto = (url || '').trim();
    if (!texto) {
      return null;
    }

    try {
      const valor = new URL(texto);
      return valor.protocol === 'http:' || valor.protocol === 'https:' ? valor.toString() : null;
    } catch {
      return null;
    }
  }

  private escaparAtributo(valor: string): string {
    return valor
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
  }
}
