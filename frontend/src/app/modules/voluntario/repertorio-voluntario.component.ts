import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { Musica } from '../../core/models/musica.models';
import { RepertorioCulto, RepertorioItem } from '../../core/models/repertorio.models';
import {
  VoluntarioRepertorioDetalhe,
  VoluntarioRepertorioListaItem
} from '../../core/models/portal-voluntario.models';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

@Component({
  selector: 'app-repertorio-voluntario',
  templateUrl: './repertorio-voluntario.component.html',
  styleUrls: ['./repertorio-voluntario.component.scss']
})
export class RepertorioVoluntarioComponent implements OnInit {
  cultos: VoluntarioRepertorioListaItem[] = [];
  detalhe: VoluntarioRepertorioDetalhe | null = null;
  repertorio: RepertorioCulto = { cultoId: 0, observacoes: null, itens: [] };
  musicas: Musica[] = [];
  carregando = true;
  carregandoMusicas = false;
  salvando = false;
  cadastrandoMusica = false;
  modalAdicionarAberto = false;
  modalCadastroAberto = false;
  musicasFiltradas: Musica[] = [];
  musicaSelecionada: Musica | null = null;
  readonly buscaMusicaControl = new FormControl('', { nonNullable: true });
  modoLista = true;

  readonly itemForm = this.fb.group({
    musicaId: [0, Validators.required],
    musicaTom: [''],
    musicaLinkCifra: [''],
    musicaLinkVideo: [''],
    responsavel: [''],
    observacoes: ['']
  });

  readonly musicaForm = this.fb.group({
    titulo: ['', [Validators.required]],
    artistaBanda: ['', [Validators.required]],
    tom: [''],
    linkCifra: [''],
    linkVideo: [''],
    observacoes: ['']
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly fb: FormBuilder,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.carregarCultos();

    this.buscaMusicaControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((termo) => {
        this.carregandoMusicas = true;
        return this.portalVoluntarioService.listarMusicasLouvor(termo.trim() || undefined).pipe(
          catchError(() => {
            this.toastr.danger('Não foi possível carregar músicas.', 'Erro');
            return of([] as Musica[]);
          })
        );
      })
    ).subscribe((res) => {
      this.musicas = res;
      this.musicasFiltradas = this.musicas;
      this.carregandoMusicas = false;
    });

    const cultoId = Number(this.route.snapshot.paramMap.get('cultoId') || 0);
    const escalaId = Number(this.route.snapshot.paramMap.get('escalaId') || 0);

    if (cultoId > 0) {
      this.modoLista = false;
      this.carregarCulto(cultoId);
      return;
    }

    if (escalaId > 0) {
      this.portalVoluntarioService.obterDetalheEscala(escalaId).subscribe({
        next: (res) => {
          if (res.item?.cultoId) {
            this.router.navigate(['/voluntario/repertorio/culto', res.item.cultoId]);
            return;
          }

          this.carregando = false;
        },
        error: () => this.carregando = false
      });
      return;
    }

    this.carregando = false;
  }

  selecionarCulto(cultoId: number): void {
    this.router.navigate(['/voluntario/repertorio/culto', cultoId]);
  }

  voltarLista(): void {
    this.router.navigate(['/voluntario/repertorio']);
  }

  buscarMusicas(): void {
    this.carregandoMusicas = true;
    this.portalVoluntarioService.listarMusicasLouvor(this.buscaMusicaControl.value.trim() || undefined).subscribe({
      next: (res) => {
        this.musicas = res;
        this.musicasFiltradas = this.musicas;
        const musicaId = Number(this.itemForm.controls.musicaId.value || 0);
        if (musicaId && !res.some((musica) => musica.id === musicaId)) {
          this.itemForm.patchValue({ musicaId: 0 });
        }
      },
      error: () => {
        this.toastr.danger('Não foi possível carregar músicas.', 'Erro');
        this.carregandoMusicas = false;
      },
      complete: () => this.carregandoMusicas = false
    });
  }

  abrirModalAdicionar(): void {
    if (!this.podeEditar()) {
      return;
    }

    this.modalAdicionarAberto = true;
    this.buscaMusicaControl.setValue('', { emitEvent: false });
    this.buscarMusicas();
  }

  fecharModalAdicionar(): void {
    this.modalAdicionarAberto = false;
    this.buscaMusicaControl.setValue('', { emitEvent: false });
    this.musicasFiltradas = [];
    this.musicaSelecionada = null;
    this.itemForm.reset({ musicaId: 0, musicaTom: '', musicaLinkCifra: '', musicaLinkVideo: '', responsavel: '', observacoes: '' });
  }

  abrirModalCadastro(): void {
    if (!this.podeEditar()) {
      return;
    }

    this.modalCadastroAberto = true;
  }

  fecharModalCadastro(): void {
    this.modalCadastroAberto = false;
    this.musicaForm.reset({ titulo: '', artistaBanda: '', tom: '', linkCifra: '', linkVideo: '', observacoes: '' });
  }

  selecionarMusicaModal(musicaId: number): void {
    const musica = this.musicas.find((item) => item.id === musicaId)
      || (this.musicaSelecionada?.id === musicaId ? this.musicaSelecionada : null);
    this.musicaSelecionada = musica;
    this.itemForm.patchValue({
      musicaId,
      musicaTom: musica?.tom || '',
      musicaLinkCifra: musica?.linkCifra || '',
      musicaLinkVideo: musica?.linkVideo || ''
    });
  }

  selecionarMusicaAutocomplete(valor: string | Musica | null | undefined): void {
    if (!valor || (typeof valor === 'string' && !valor.trim())) {
      return;
    }

    const texto = typeof valor === 'string' ? valor : this.textoMusica(valor);
    const musica = typeof valor === 'string'
      ? this.musicas.find((item) => this.textoMusica(item) === texto)
        || this.musicasFiltradas.find((item) => this.textoMusica(item) === texto)
      : valor;
    if (!musica) {
      this.toastr.warning('Não foi possível identificar a música selecionada.', 'Repertório');
      return;
    }

    this.buscaMusicaControl.setValue(texto, { emitEvent: false });
    this.musicaSelecionada = musica;
    this.selecionarMusicaModal(musica.id);
  }

  mostrarTodasMusicas(): void {
    if (!this.buscaMusicaControl.value.trim()) {
      this.musicasFiltradas = this.musicas;
    }
  }

  textoMusica(musica: Musica): string {
    return `${musica.titulo} - ${musica.artistaBanda}`;
  }

  musicaEstaSelecionada(musicaId: number): boolean {
    return Number(this.itemForm.controls.musicaId.value || 0) === musicaId;
  }

  get musicasDisponiveis(): Musica[] {
    const idsNoRepertorio = new Set(this.repertorio.itens.map((item) => item.musicaId));
    return this.musicasFiltradas.filter((musica) => !idsNoRepertorio.has(musica.id));
  }

  adicionarItem(): void {
    if (!this.podeEditar()) {
      return;
    }

    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const raw = this.itemForm.getRawValue();
    const musica = this.musicaSelecionada?.id === Number(raw.musicaId)
      ? this.musicaSelecionada
      : this.musicas.find((item) => item.id === Number(raw.musicaId));
    if (!musica) {
      this.toastr.warning('Selecione novamente uma música válida.', 'Repertório');
      return;
    }

    if (!this.linksValidos(raw.musicaLinkCifra, raw.musicaLinkVideo)) {
      this.toastr.warning('Informe links válidos usando http ou https.', 'Repertório');
      return;
    }

    if (this.repertorio.itens.some((item) => item.musicaId === musica.id)) {
      this.toastr.warning('Esta música já está no repertório.', 'Repertório');
      return;
    }

    const item: RepertorioItem = {
      musicaId: musica.id,
      musicaTitulo: musica.titulo,
      musicaArtistaBanda: musica.artistaBanda,
      musicaTom: raw.musicaTom || musica.tom,
      musicaLinkCifra: this.normalizarLink(raw.musicaLinkCifra) || musica.linkCifra,
      musicaLinkVideo: this.normalizarLink(raw.musicaLinkVideo) || musica.linkVideo,
      musicaObservacoes: musica.observacoes,
      etapaCultoId: null,
      ordem: this.repertorio.itens.length + 1,
      responsavel: raw.responsavel || null,
      observacoes: raw.observacoes || null
    };

    this.repertorio.itens = [...this.repertorio.itens, item];
    this.musicaSelecionada = null;
    this.itemForm.reset({ musicaId: 0, musicaTom: '', musicaLinkCifra: '', musicaLinkVideo: '', responsavel: '', observacoes: '' });
    this.modalAdicionarAberto = false;
  }

  removerItem(index: number): void {
    if (!this.podeEditar()) {
      return;
    }

    this.repertorio.itens = this.repertorio.itens
      .filter((_, itemIndex) => itemIndex !== index)
      .map((item, itemIndex) => ({ ...item, ordem: itemIndex + 1 }));
  }

  moverItem(index: number, deslocamento: number): void {
    if (!this.podeEditar()) {
      return;
    }

    const destino = index + deslocamento;
    if (destino < 0 || destino >= this.repertorio.itens.length) {
      return;
    }

    const itens = [...this.repertorio.itens];
    [itens[index], itens[destino]] = [itens[destino], itens[index]];
    this.repertorio.itens = itens.map((item, itemIndex) => ({ ...item, ordem: itemIndex + 1 }));
  }

  cadastrarMusica(): void {
    if (!this.podeEditar()) {
      return;
    }

    if (this.musicaForm.invalid) {
      this.musicaForm.markAllAsTouched();
      return;
    }

    const dados = this.musicaForm.getRawValue();
    const scaleId = this.detalhe?.escalaGerenciavelId;
    if (!scaleId) {
      this.toastr.warning('Sem escala autorizada para cadastrar música.', 'Repertório');
      return;
    }

    this.cadastrandoMusica = true;
    this.portalVoluntarioService.cadastrarMusicaDaEscala(scaleId, {
      titulo: dados.titulo || '',
      artistaBanda: dados.artistaBanda || '',
      tom: dados.tom || null,
      linkCifra: dados.linkCifra || null,
      linkVideo: dados.linkVideo || null,
      observacoes: dados.observacoes || null
    }).subscribe({
      next: (musica) => {
        this.musicas = [...this.musicas, musica].sort((a, b) => a.titulo.localeCompare(b.titulo));
        this.modalCadastroAberto = false;
        this.musicaForm.reset({ titulo: '', artistaBanda: '', tom: '', linkCifra: '', linkVideo: '', observacoes: '' });
        this.toastr.success('Música cadastrada.', 'Músicas');
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível cadastrar a música.', 'Erro');
        this.cadastrandoMusica = false;
      },
      complete: () => this.cadastrandoMusica = false
    });
  }

  salvar(): void {
    if (!this.podeEditar()) {
      return;
    }

    const scaleId = this.detalhe?.escalaGerenciavelId;
    if (!scaleId) {
      this.toastr.warning('Sem escala autorizada para salvar repertório.', 'Repertório');
      return;
    }

    this.salvando = true;
    this.portalVoluntarioService.salvarRepertorioDaEscala(scaleId, {
      cultoId: this.repertorio.cultoId,
      observacoes: this.repertorio.observacoes || null,
      itens: this.repertorio.itens.map((item, index) => ({ ...item, ordem: index + 1 }))
    }).subscribe({
      next: (res) => this.toastr.success(res.mensagem, 'Repertório'),
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível salvar o repertório.', 'Erro');
        this.salvando = false;
      },
      complete: () => this.salvando = false
    });
  }

  podeEditar(): boolean {
    return this.detalhe?.podeGerenciar === true && !!this.detalhe?.escalaGerenciavelId;
  }

  carregarCultos(): void {
    this.portalVoluntarioService.listarRepertorios().subscribe({
      next: (res) => this.cultos = res,
      error: () => {
        this.toastr.danger('Não foi possível carregar os cultos.', 'Erro');
        this.carregando = false;
      },
      complete: () => {
        if (this.modoLista) {
          this.carregando = false;
        }
      }
    });
  }

  private carregarCulto(cultoId: number): void {
    this.portalVoluntarioService.obterRepertorioCulto(cultoId).subscribe({
      next: (res) => {
        this.detalhe = res;
        this.repertorio = res.repertorio
          ? {
              id: res.repertorio.id,
              cultoId: res.repertorio.cultoId,
              observacoes: res.repertorio.observacoes || null,
              itens: (res.repertorio.itens || []).slice().sort((a, b) => a.ordem - b.ordem).map((item) => ({
                id: item.id,
                musicaId: item.musicaId,
                musicaTitulo: item.musicaTitulo || undefined,
                musicaArtistaBanda: item.musicaArtistaBanda,
                musicaTom: item.musicaTom,
                musicaLinkCifra: item.musicaLinkCifra,
                musicaLinkVideo: item.musicaLinkVideo,
                musicaObservacoes: item.musicaObservacoes,
                etapaCultoId: item.etapaCultoId,
                etapaAtividade: item.etapaAtividade,
                ordem: item.ordem,
                responsavel: item.responsavel,
                observacoes: item.observacoes
              }))
            }
          : { cultoId: res.cultoId, observacoes: null, itens: [] };
        this.modoLista = false;
        this.buscarMusicas();
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível carregar o repertório.', 'Erro');
        this.modoLista = true;
        this.carregando = false;
      },
      complete: () => this.carregando = false
    });
  }

  private normalizarLink(valor: string | null | undefined): string | null {
    const texto = String(valor || '').trim();
    return texto || null;
  }

  private linksValidos(linkCifra: string | null | undefined, linkVideo: string | null | undefined): boolean {
    return [linkCifra, linkVideo].every((link) => {
      const texto = this.normalizarLink(link);
      if (!texto) {
        return true;
      }

      try {
        const url = new URL(texto);
        return url.protocol === 'http:' || url.protocol === 'https:';
      } catch {
        return false;
      }
    });
  }
}
