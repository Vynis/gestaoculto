import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { VoluntarioAuthService } from '../../core/services/voluntario-auth.service';

@Component({
  selector: 'app-recuperar-acesso-voluntario',
  templateUrl: './recuperar-acesso-voluntario.component.html',
  styleUrls: ['./recuperar-acesso-voluntario.component.scss']
})
export class RecuperarAcessoVoluntarioComponent {
  carregando = false;
  codigoGerado = '';

  readonly solicitarForm = this.fb.group({
    identificador: ['', Validators.required]
  });

  readonly redefinirForm = this.fb.group({
    identificador: ['', Validators.required],
    codigo: ['', Validators.required],
    novaSenha: ['', [Validators.required, Validators.minLength(6)]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly voluntarioAuthService: VoluntarioAuthService,
    private readonly toastr: NbToastrService
  ) {}

  solicitarCodigo(): void {
    if (this.solicitarForm.invalid) {
      this.solicitarForm.markAllAsTouched();
      return;
    }

    this.carregando = true;
    const identificador = this.solicitarForm.controls.identificador.value || '';
    this.voluntarioAuthService.solicitarRecuperacao(identificador).subscribe({
      next: (res) => {
        this.codigoGerado = res.codigo;
        this.redefinirForm.patchValue({ identificador, codigo: res.codigo });
        this.toastr.success('Código de recuperação gerado.', 'Recuperação');
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
    if (this.redefinirForm.invalid) {
      this.redefinirForm.markAllAsTouched();
      return;
    }

    this.carregando = true;
    this.voluntarioAuthService.redefinirAcesso(this.redefinirForm.getRawValue() as any).subscribe({
      next: () => {
        this.toastr.success('Senha atualizada com sucesso. Faça login novamente.', 'Sucesso');
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
