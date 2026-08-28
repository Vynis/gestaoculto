import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { CellClickedEvent, ColDef, ICellRendererParams } from 'ag-grid-community';
import { NbToastrService } from '@nebular/theme';
import { Culto } from '../../core/models/culto.models';
import { EtapaCulto } from '../../core/models/cronograma.models';
import { Musica } from '../../core/models/musica.models';
import { RepertorioCulto, RepertorioItem } from '../../core/models/repertorio.models';
import { CronogramaService } from '../../core/services/cronograma.service';
import { CultoService } from '../../core/services/culto.service';
import { MusicaService } from '../../core/services/musica.service';
import { RepertorioService } from '../../core/services/repertorio.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

@Component({
  selector: 'app-repertorio',
  templateUrl: './repertorio.component.html',
  styleUrls: ['./repertorio.component.scss']
})
export class RepertorioComponent implements OnInit {
  cultos: Culto[] = [];
  musicas: Musica[] = [];
  etapas: EtapaCulto[] = [];
  repertorio: RepertorioCulto = { cultoId: 0, observacoes: null, itens: [] };
  rowData: RepertorioGridRow[] = [];
  filtroGrid = '';
  modalItemAberto = false;
  itemEditandoIndex: number | null = null;
  carregandoMusicas = false;
  musicasFiltradas: Musica[] = [];
  readonly buscaMusicaControl = new FormControl('', { nonNullable: true });

  readonly defaultColDef: ColDef<RepertorioGridRow> = {
    sortable: true,
    filter: true,
    resizable: true,
    flex: 1,
    minWidth: 110
  };

  readonly columnDefs: ColDef<RepertorioGridRow>[] = [
    { headerName: 'Ordem', field: 'ordem', minWidth: 90, maxWidth: 110 },
    { headerName: 'Música', field: 'musica', minWidth: 190 },
    { headerName: 'Artista', field: 'artista', minWidth: 170 },
    { headerName: 'Tom', field: 'tom', minWidth: 100, maxWidth: 120 },
    { headerName: 'Etapa', field: 'etapa', minWidth: 150 },
    { headerName: 'Responsável', field: 'responsavel', minWidth: 150 },
    {
      headerName: 'Cifra',
      field: 'linkCifra',
      minWidth: 110,
      maxWidth: 130,
      cellRenderer: (params: ICellRendererParams<RepertorioGridRow>) => params.value
        ? `<a class="grid-link" href="${params.value}" target="_blank" rel="noopener noreferrer">Abrir</a>`
        : '-'
    },
    {
      headerName: 'Vídeo',
      field: 'linkVideo',
      minWidth: 110,
      maxWidth: 130,
      cellRenderer: (params: ICellRendererParams<RepertorioGridRow>) => params.value
        ? `<a class="grid-link" href="${params.value}" target="_blank" rel="noopener noreferrer">Abrir</a>`
        : '-'
    },
    {
      headerName: 'Ações',
      field: 'repertorioItem',
      minWidth: 190,
      maxWidth: 240,
      sortable: false,
      filter: false,
      cellRenderer: () =>
        '<div class="grid-actions"><button class="grid-btn icon edit" data-action="editar" type="button" title="Editar item" aria-label="Editar item"><span class="icon-pencil"></span></button><button class="grid-btn" data-action="subir" type="button" title="Subir item" aria-label="Subir item">↑</button><button class="grid-btn" data-action="descer" type="button" title="Descer item" aria-label="Descer item">↓</button><button class="grid-btn icon delete" data-action="excluir" type="button" title="Excluir item" aria-label="Excluir item"><span class="icon-trash"></span></button></div>'
    }
  ];

  readonly paginationPageSize = 8;

  readonly localeText = {
    noRowsToShow: 'Nenhum item no repertório.'
  };

  readonly filtro = this.fb.group({
    cultoId: [0, Validators.required]
  });

  readonly itemForm = this.fb.group({
    musicaId: [0, [Validators.required, Validators.min(1)]],
    musicaTitulo: [''],
    musicaArtistaBanda: [''],
    musicaTom: [''],
    musicaLinkCifra: [''],
    musicaLinkVideo: [''],
    musicaObservacoes: [''],
    etapaCultoId: [null as number | null],
    responsavel: [''],
    observacoes: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly cultoService: CultoService,
    private readonly cronogramaService: CronogramaService,
    private readonly musicaService: MusicaService,
    private readonly repertorioService: RepertorioService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.filtro.controls.cultoId.valueChanges.subscribe((valor) => {
      const cultoId = Number(valor || 0);
      this.etapas = [];
      this.itemForm.patchValue({ etapaCultoId: null });

      if (!cultoId) {
        return;
      }

      this.carregarEtapasDoCulto(cultoId);
    });

    this.itemForm.controls.musicaId.valueChanges.subscribe((valor) => {
      const musicaId = Number(valor || 0);
      if (!musicaId) {
        this.itemForm.patchValue({
          musicaTitulo: '',
          musicaArtistaBanda: '',
          musicaTom: '',
          musicaLinkCifra: '',
          musicaLinkVideo: '',
          musicaObservacoes: ''
        }, { emitEvent: false });
        return;
      }

      const musica = this.musicas.find((x) => x.id === musicaId);
      if (!musica) {
        return;
      }

      this.itemForm.patchValue({
        musicaTitulo: musica.titulo || '',
        musicaArtistaBanda: musica.artistaBanda || '',
        musicaTom: musica.tom || '',
        musicaLinkCifra: musica.linkCifra || '',
        musicaLinkVideo: musica.linkVideo || '',
        musicaObservacoes: musica.observacoes || ''
      }, { emitEvent: false });
    });

    this.buscaMusicaControl.valueChanges.subscribe((valor) => {
      const termo = valor.trim().toLowerCase();
      this.musicasFiltradas = !termo
        ? this.musicas
        : this.musicas.filter((musica) =>
          musica.titulo.toLowerCase().includes(termo)
          || musica.artistaBanda.toLowerCase().includes(termo));

      const musicaSelecionada = this.musicas.find((musica) => this.textoMusica(musica) === valor);
      if (!musicaSelecionada && this.itemForm.controls.musicaId.value) {
        this.itemForm.patchValue({ musicaId: 0 });
      }
    });

    this.cultoService.listarAtivos().subscribe((data) => {
      this.cultos = data;
    });

    this.carregarMusicas();
  }

  carregar(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.etapas = [];
      return;
    }

    this.carregarEtapasDoCulto(cultoId);

    this.repertorioService.obterPorCulto(cultoId).subscribe((data) => {
      this.repertorio = {
        ...data,
        cultoId,
        itens: (data.itens || []).slice().sort((a, b) => a.ordem - b.ordem)
      };
      this.atualizarGrid();
    });
  }

  adicionarItem(): void {
    this.abrirModalNovoItem();
  }

  abrirModalNovoItem(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.toastr.warning('Selecione um culto.', 'Repertório');
      return;
    }

    this.itemEditandoIndex = null;
    this.limparFormularioItem();
    this.modalItemAberto = true;
    this.carregarMusicas();
  }

  abrirModalEditarItem(index: number): void {
    const item = this.repertorio.itens[index];
    if (!item) {
      return;
    }

    this.itemEditandoIndex = index;
    this.modalItemAberto = true;
    this.itemForm.reset({
      musicaId: item.musicaId,
      musicaTitulo: item.musicaTitulo || '',
      musicaArtistaBanda: item.musicaArtistaBanda || '',
      musicaTom: item.musicaTom || '',
      musicaLinkCifra: item.musicaLinkCifra || '',
      musicaLinkVideo: item.musicaLinkVideo || '',
      musicaObservacoes: item.musicaObservacoes || '',
      etapaCultoId: item.etapaCultoId,
      responsavel: item.responsavel || '',
      observacoes: item.observacoes || ''
    }, { emitEvent: false });
    const musica = this.musicas.find((atual) => atual.id === item.musicaId);
    this.buscaMusicaControl.setValue(musica ? this.textoMusica(musica) : item.musicaTitulo || '', { emitEvent: false });
  }

  fecharModalItem(): void {
    this.modalItemAberto = false;
    this.itemEditandoIndex = null;
    this.limparFormularioItem();
  }

  selecionarMusicaAutocomplete(texto: string): void {
    const musica = this.musicas.find((item) => this.textoMusica(item) === texto);
    if (!musica) {
      return;
    }

    this.buscaMusicaControl.setValue(texto, { emitEvent: false });
    this.itemForm.patchValue({ musicaId: musica.id });
  }

  salvarItemModal(): void {
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const raw = this.itemForm.getRawValue();
    if (Number(raw.musicaId || 0) <= 0) {
      this.itemForm.controls.musicaId.setErrors({ required: true });
      this.itemForm.controls.musicaId.markAsTouched();
      return;
    }
    const musica = this.musicas.find((x) => x.id === Number(raw.musicaId));
    const etapa = this.etapas.find((x) => x.id === Number(raw.etapaCultoId));

    const item: RepertorioItem = {
      musicaId: Number(raw.musicaId),
      musicaTitulo: this.normalizarTexto(raw.musicaTitulo) || musica?.titulo,
      musicaArtistaBanda: this.normalizarTexto(raw.musicaArtistaBanda),
      musicaTom: this.normalizarTexto(raw.musicaTom),
      musicaLinkCifra: this.normalizarTexto(raw.musicaLinkCifra),
      musicaLinkVideo: this.normalizarTexto(raw.musicaLinkVideo),
      musicaObservacoes: this.normalizarTexto(raw.musicaObservacoes),
      etapaCultoId: raw.etapaCultoId,
      etapaAtividade: etapa?.atividade,
      ordem: this.repertorio.itens.length + 1,
      responsavel: raw.responsavel || null,
      observacoes: raw.observacoes || null
    };

    if (this.itemEditandoIndex === null) {
      this.repertorio.itens = [...this.repertorio.itens, item];
    } else {
      this.repertorio.itens = this.repertorio.itens.map((atual, index) => index === this.itemEditandoIndex ? { ...atual, ...item, ordem: atual.ordem } : atual);
    }

    this.recalcularOrdens();
    this.atualizarGrid();
    this.fecharModalItem();
  }

  async removerItem(index: number): Promise<void> {
    const item = this.repertorio.itens[index];
    const nome = item?.musicaTitulo || `Música #${item?.musicaId || index + 1}`;
    const confirmou = await confirmarExclusao(`Deseja excluir o item "${nome}" do repertório?`);
    if (!confirmou) {
      return;
    }

    this.repertorio.itens.splice(index, 1);
    this.recalcularOrdens();
    this.atualizarGrid();
  }

  moverItem(index: number, deslocamento: number): void {
    const novoIndex = index + deslocamento;
    if (novoIndex < 0 || novoIndex >= this.repertorio.itens.length) {
      return;
    }

    const itens = [...this.repertorio.itens];
    const [item] = itens.splice(index, 1);
    itens.splice(novoIndex, 0, item);
    this.repertorio.itens = itens;
    this.recalcularOrdens();
    this.atualizarGrid();
  }

  onGridCellClicked(event: CellClickedEvent<RepertorioGridRow>): void {
    const target = event.event?.target as HTMLElement | null;
    const action = target?.closest('button[data-action]')?.getAttribute('data-action');
    if (!action || !event.data) {
      return;
    }

    const index = this.repertorio.itens.indexOf(event.data.repertorioItem);
    if (index < 0) {
      return;
    }

    if (action === 'editar') {
      this.abrirModalEditarItem(index);
      return;
    }

    if (action === 'subir') {
      this.moverItem(index, -1);
      return;
    }

    if (action === 'descer') {
      this.moverItem(index, 1);
      return;
    }

    if (action === 'excluir') {
      this.removerItem(index);
    }
  }

  atualizarEtapaDoItem(item: RepertorioItem, etapaCultoId: number | null): void {
    const etapaId = etapaCultoId ? Number(etapaCultoId) : null;
    item.etapaCultoId = etapaId;
    item.etapaAtividade = this.etapas.find((x) => x.id === etapaId)?.atividade || null;
  }

  salvar(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.toastr.warning('Selecione um culto.', 'Repertório');
      return;
    }

    this.repertorioService.salvar({
      cultoId,
      observacoes: this.repertorio.observacoes || null,
      itens: this.repertorio.itens.map((item, index) => ({
        ...item,
        ordem: index + 1
      }))
    }).subscribe(() => {
      this.toastr.success('Repertório salvo com sucesso.', 'Repertório');
      this.carregar();
    });
  }

  duplicarCultoAnterior(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      return;
    }

    this.repertorioService.duplicarDoCultoAnterior(cultoId).subscribe({
      next: () => {
        this.toastr.success('Repertório duplicado do culto anterior.', 'Repertório');
        this.carregar();
      },
      error: (error) => {
        this.toastr.warning(error?.error?.mensagem || 'Não foi possível duplicar repertório.', 'Repertório');
      }
    });
  }

  private carregarEtapasDoCulto(cultoId: number): void {
    this.cronogramaService.listarPorCulto(cultoId).subscribe((etapas) => {
      const etapasUnicasPorAssinatura = new Map<string, EtapaCulto>();

      (etapas || [])
        .filter((item) => item.cultoId === cultoId)
        .filter((item, index, arr) => arr.findIndex((x) => x.id === item.id) === index)
        .sort((a, b) => a.sequencia - b.sequencia || a.id - b.id)
        .forEach((item) => {
          const chave = this.assinaturaEtapa(item);
          if (!etapasUnicasPorAssinatura.has(chave)) {
            etapasUnicasPorAssinatura.set(chave, item);
          }
        });

      this.etapas = Array.from(etapasUnicasPorAssinatura.values())
        .sort((a, b) => a.sequencia - b.sequencia || a.id - b.id);

      const etapaSelecionada = Number(this.itemForm.value.etapaCultoId || 0);
      if (etapaSelecionada && !this.etapas.some((item) => item.id === etapaSelecionada)) {
        this.itemForm.patchValue({ etapaCultoId: null });
      }
    });
  }

  private carregarMusicas(): void {
    this.carregandoMusicas = true;
    this.musicaService.listar(undefined, true).subscribe({
      next: (data) => {
        this.musicas = data || [];
        this.atualizarMusicasFiltradas();
      },
      error: () => {
        this.musicas = [];
        this.carregandoMusicas = false;
        this.toastr.danger('Não foi possível carregar as músicas ativas.', 'Repertório');
      },
      complete: () => this.carregandoMusicas = false
    });
  }

  private assinaturaEtapa(item: EtapaCulto): string {
    const sequencia = Number(item.sequencia || 0);
    const atividade = String(item.atividade || '').trim().toLowerCase();
    return `${sequencia}|${atividade}`;
  }

  private normalizarTexto(valor: string | null | undefined): string | null {
    const texto = String(valor || '').trim();
    return texto ? texto : null;
  }

  private limparFormularioItem(): void {
    this.buscaMusicaControl.setValue('', { emitEvent: false });
    this.itemForm.reset({
      musicaId: 0,
      musicaTitulo: '',
      musicaArtistaBanda: '',
      musicaTom: '',
      musicaLinkCifra: '',
      musicaLinkVideo: '',
      musicaObservacoes: '',
      etapaCultoId: null,
      responsavel: '',
      observacoes: ''
    });
  }

  private recalcularOrdens(): void {
    this.repertorio.itens = this.repertorio.itens.map((item, index) => ({ ...item, ordem: index + 1 }));
  }

  private atualizarGrid(): void {
    this.rowData = this.repertorio.itens.map((item) => ({
      ordem: item.ordem,
      musica: item.musicaTitulo || `Música #${item.musicaId}`,
      artista: item.musicaArtistaBanda || '-',
      tom: item.musicaTom || '-',
      etapa: item.etapaAtividade || '-',
      responsavel: item.responsavel || '-',
      linkCifra: item.musicaLinkCifra || '',
      linkVideo: item.musicaLinkVideo || '',
      repertorioItem: item
    }));
  }

  private atualizarMusicasFiltradas(): void {
    const termo = this.buscaMusicaControl.value.trim().toLowerCase();
    this.musicasFiltradas = !termo
      ? this.musicas
      : this.musicas.filter((musica) =>
        musica.titulo.toLowerCase().includes(termo)
        || musica.artistaBanda.toLowerCase().includes(termo));
  }

  textoMusica(musica: Musica): string {
    return `${musica.titulo} - ${musica.artistaBanda}`;
  }
}

interface RepertorioGridRow {
  ordem: number;
  musica: string;
  artista: string;
  tom: string;
  etapa: string;
  responsavel: string;
  linkCifra: string;
  linkVideo: string;
  repertorioItem: RepertorioItem;
}
