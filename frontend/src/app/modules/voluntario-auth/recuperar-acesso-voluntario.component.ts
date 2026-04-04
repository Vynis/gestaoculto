import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NbToastrService } from '@nebular/theme';
import { VoluntarioAuthService } from '../../core/services/voluntario-auth.service';

@Component({
  selector: 'app-recuperar-acesso-voluntario',
  templateUrl: './recuperar-acesso-voluntario.component.html',
  styleUrls: ['./recuperar-acesso-voluntario.component.scss']
})
export class RecuperarAcessoVoluntarioComponent {
  carregando = false;
  token = '';

  readonly solicitarForm = this.fb.group({
    identificador: ['', Validators.required]
  });

  readonly redefinirForm = this.fb.group({
    novaSenha: ['', [Validators.required, Validators.minLength(6)]],
    confirmarSenha: ['', [Validators.required, Validators.minLength(6)]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly voluntarioAuthService: VoluntarioAuthService,
    private readonly toastr: NbToastrService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    this.route.queryParamMap.subscribe((params) => {
      this.token = (params.get('token') || '').trim();
    });
  }

  solicitarCodigo(): void {
    if (this.solicitarForm.invalid) {
      this.solicitarForm.markAllAsTouched();
      return;
    }

    this.carregando = true;
    const identificador = this.solicitarForm.controls.identificador.value || '';
    this.voluntarioAuthService.solicitarRecuperacao(identificador).subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem, 'Recuperação');
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Não foi possível gerar o código.', 'Atenção');
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
    this.voluntarioAuthService.redefinirAcessoPorToken(this.token, raw.novaSenha || '').subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem, 'Sucesso');
        this.router.navigate(['/voluntario/login']);
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Falha ao redefinir senha.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }
}
