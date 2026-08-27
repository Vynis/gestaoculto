import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NbToastrService } from '@nebular/theme';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-recuperar-senha',
  templateUrl: './recuperar-senha.component.html',
  styleUrls: ['./recuperar-senha.component.scss']
})
export class RecuperarSenhaComponent implements OnInit {
  carregando = false;
  token = '';

  readonly solicitarForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  readonly redefinirForm = this.fb.group({
    novaSenha: ['', [Validators.required, Validators.minLength(6)]],
    confirmarSenha: ['', [Validators.required, Validators.minLength(6)]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.token = (params.get('token') || '').trim();
    });
  }

  solicitarLink(): void {
    if (this.solicitarForm.invalid) {
      this.solicitarForm.markAllAsTouched();
      return;
    }

    this.carregando = true;
    const email = this.solicitarForm.controls.email.value || '';
    this.authService.solicitarRecuperacaoSenha(email).subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem, 'Recuperação de senha');
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Não foi possível solicitar a recuperação.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  redefinir(): void {
    if (!this.token) {
      this.toastr.warning('Link de recuperação inválido.', 'Atenção');
      return;
    }

    if (this.redefinirForm.invalid) {
      this.redefinirForm.markAllAsTouched();
      return;
    }

    const raw = this.redefinirForm.getRawValue();
    if (raw.novaSenha !== raw.confirmarSenha) {
      this.toastr.warning('As senhas não conferem.', 'Atenção');
      return;
    }

    this.carregando = true;
    this.authService.redefinirSenhaPorToken(this.token, raw.novaSenha || '').subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem, 'Recuperação de senha');
        this.router.navigate(['/auth/login']);
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Não foi possível redefinir a senha.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }
}
