import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NbToastrService } from '@nebular/theme';
import { AuthService } from '../../core/services/auth.service';
import { VoluntarioAuthService } from '../../core/services/voluntario-auth.service';

@Component({
  selector: 'app-primeiro-acesso-voluntario',
  templateUrl: './primeiro-acesso-voluntario.component.html',
  styleUrls: ['./primeiro-acesso-voluntario.component.scss']
})
export class PrimeiroAcessoVoluntarioComponent {
  carregando = false;
  codigoGerado = '';

  readonly formSolicitar = this.fb.group({
    identificador: ['', Validators.required]
  });

  readonly formAtivar = this.fb.group({
    identificador: ['', Validators.required],
    codigo: ['', Validators.required],
    nome: [''],
    email: [''],
    telefone: [''],
    senha: ['', [Validators.required, Validators.minLength(6)]],
    manterConectado: [true]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly voluntarioAuthService: VoluntarioAuthService,
    private readonly authService: AuthService,
    private readonly toastr: NbToastrService,
    private readonly router: Router
  ) {}

  solicitarCodigo(): void {
    if (this.formSolicitar.invalid) {
      this.formSolicitar.markAllAsTouched();
      return;
    }

    this.carregando = true;
    const identificador = this.formSolicitar.controls.identificador.value || '';
    this.voluntarioAuthService.solicitarAtivacao(identificador).subscribe({
      next: (res) => {
        this.codigoGerado = res.codigo;
        this.formAtivar.patchValue({ identificador, codigo: res.codigo });
        this.toastr.success('Código gerado. Use para ativar seu acesso.', 'Primeiro acesso');
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Não foi possível gerar o código.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  ativar(): void {
    if (this.formAtivar.invalid) {
      this.formAtivar.markAllAsTouched();
      return;
    }

    this.carregando = true;
    this.voluntarioAuthService.ativarAcesso(this.formAtivar.getRawValue() as any).subscribe({
      next: (res) => {
        this.authService.aplicarSessao(res);
        this.toastr.success('Acesso ativado com sucesso.', 'Bem-vindo');
        this.router.navigate(['/voluntario/painel']);
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Não foi possível ativar o acesso.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }
}
