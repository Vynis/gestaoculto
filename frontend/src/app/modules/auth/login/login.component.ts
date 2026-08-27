import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NbToastrService } from '@nebular/theme';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  carregando = false;

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required]],
    manterConectado: [true]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly toastr: NbToastrService,
    private readonly router: Router
  ) {}

  entrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.carregando = true;
    const value = this.form.getRawValue();
    this.authService.login(value.email!, value.senha!, value.manterConectado ?? true).subscribe({
      next: () => {
        this.toastr.success('Acesso liberado com sucesso.', 'Bem-vindo');
        this.router.navigate([this.authService.destinoPosLogin('gestao')]);
      },
      error: (error) => {
        this.carregando = false;
        this.toastr.danger(error?.error?.mensagem ?? 'Não foi possível entrar.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  async entrarComGoogle(): Promise<void> {
    this.carregando = true;
    try {
      const manter = this.form.controls.manterConectado.value ?? true;
      await this.authService.loginComGoogle(manter);
      this.toastr.success('Login Google concluído.', 'Bem-vindo');
      await this.router.navigate([this.authService.destinoPosLogin('gestao')]);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Falha no login com Google.';
      this.toastr.danger(message, 'Atenção');
    } finally {
      this.carregando = false;
    }
  }
}
