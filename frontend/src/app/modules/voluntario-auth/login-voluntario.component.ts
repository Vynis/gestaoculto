import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NbToastrService } from '@nebular/theme';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-voluntario',
  templateUrl: './login-voluntario.component.html',
  styleUrls: ['./login-voluntario.component.scss']
})
export class LoginVoluntarioComponent {
  carregando = false;

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', Validators.required],
    manterConectado: [true]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly toastr: NbToastrService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  private obterReturnUrl(): string {
    const retorno = this.route.snapshot.queryParamMap.get('returnUrl') || '';
    return retorno.startsWith('/voluntario/') ? retorno : this.authService.destinoPosLogin('voluntario');
  }

  entrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.carregando = true;

    this.authService.login(raw.email!, raw.senha!, raw.manterConectado ?? true).subscribe({
      next: () => {
        if (!this.authService.ehVoluntario()) {
          this.toastr.warning('Seu acesso não é de voluntário. Use a área de gestão.', 'Atenção');
          this.router.navigate([this.authService.destinoPosLogin('gestao')]);
          return;
        }

        this.toastr.success('Bem-vindo ao portal do voluntário.', 'Sucesso');
        this.router.navigateByUrl(this.obterReturnUrl());
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
}
