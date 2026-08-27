import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { Musica, MusicaRequest } from '../../core/models/musica.models';
import { MusicaService } from '../../core/services/musica.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

@Component({
  selector: 'app-musicas',
  templateUrl: './musicas.component.html',
  styleUrls: ['./musicas.component.scss']
})
export class MusicasComponent implements OnInit {
  musicas: Musica[] = [];
  modalAberto = false;
  musicaEditandoId: number | null = null;
  termoBusca = '';
  filtroAtivo: '' | 'true' | 'false' = '';

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
    this.musicaService.listar(this.termoBusca || undefined, ativo).subscribe((data) => {
      this.musicas = data;
    });
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
}
