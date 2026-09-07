import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NbToastrService } from '@nebular/theme';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-trocar-senha-obrigatoria',
  templateUrl: './trocar-senha-obrigatoria.component.html',
  styleUrls: ['./trocar-senha-obrigatoria.component.scss']
})
export class TrocarSenhaObrigatoriaComponent {
  carregando = false;

  readonly form = this.fb.group({
    novaSenha: ['', [Validators.required, Validators.minLength(6)]],
    confirmarSenha: ['', [Validators.required, Validators.minLength(6)]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly toastr: NbToastrService,
    private readonly router: Router
  ) {}

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    if (raw.novaSenha !== raw.confirmarSenha) {
      this.toastr.warning('As senhas não conferem.', 'Atenção');
      return;
    }

    this.carregando = true;
    this.authService.trocarSenhaObrigatoria(raw.novaSenha || '').subscribe({
      next: (response) => {
        this.toastr.success(response.mensagem, 'Senha alterada');
        this.router.navigate([this.authService.destinoPadraoPosLogin()]);
      },
      error: (error) => {
        this.carregando = false;
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível alterar a senha.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  sair(): void {
    this.authService.logout();
  }
}
