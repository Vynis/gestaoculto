import { Component, Input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-external-link-viewer',
  templateUrl: './external-link-viewer.component.html',
  styleUrls: ['./external-link-viewer.component.scss']
})
export class ExternalLinkViewerComponent {
  private _url: string | null = null;

  @Input() label = 'Abrir link';
  @Input() mode: 'modal' | 'blank' = 'blank';

  safeUrl: SafeResourceUrl | null = null;
  urlNormalizada: string | null = null;
  modalAberto = false;

  @Input()
  set url(value: string | null | undefined) {
    this._url = this.normalizarUrl(value);
    this.urlNormalizada = this._url;
    this.safeUrl = this._url ? this.sanitizer.bypassSecurityTrustResourceUrl(this._url) : null;
  }

  get url(): string | null {
    return this._url;
  }

  constructor(private readonly sanitizer: DomSanitizer) {}

  abrirModal(): void {
    if (!this.urlNormalizada) {
      return;
    }

    this.modalAberto = true;
  }

  fecharModal(): void {
    this.modalAberto = false;
  }

  private normalizarUrl(value: string | null | undefined): string | null {
    const texto = (value || '').trim();
    if (!texto) {
      return null;
    }

    try {
      const url = new URL(texto);
      return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
    } catch {
      return null;
    }
  }
}
