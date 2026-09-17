import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { ImportacaoMusicaResult } from '../core/models/musica.models';
import { MusicaService } from '../core/services/musica.service';
import { PortalVoluntarioService } from '../core/services/portal-voluntario.service';

@Component({
  selector: 'app-importar-youtube-modal',
  templateUrl: './importar-youtube-modal.component.html',
  styleUrls: ['./importar-youtube-modal.component.scss']
})
export class ImportarYoutubeModalComponent {
  @Input() contexto: 'admin' | 'voluntario' = 'admin';
  @Output() resultado = new EventEmitter<ImportacaoMusicaResult>();
  @Output() cancelar = new EventEmitter<void>();

  carregando = false;
  readonly form = this.fb.group({
    linkVideo: ['', Validators.required]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly musicaService: MusicaService,
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly toastr: NbToastrService
  ) {}

  carregar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.carregando = true;
    const linkVideo = (this.form.controls.linkVideo.value || '').trim();
    const requisicao = this.contexto === 'voluntario'
      ? this.portalVoluntarioService.importarYoutube(linkVideo)
      : this.musicaService.importarYoutube(linkVideo);

    requisicao.subscribe({
      next: (res) => this.resultado.emit(res),
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível carregar os dados do vídeo.', 'Importação');
        this.carregando = false;
      },
      complete: () => this.carregando = false
    });
  }

  fechar(): void {
    if (!this.carregando) {
      this.cancelar.emit();
    }
  }
}
