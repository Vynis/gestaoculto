import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { PortalVoluntarioService } from '../../core/services/portal-voluntario.service';

@Component({
  selector: 'app-meus-dados-voluntario',
  templateUrl: './meus-dados-voluntario.component.html',
  styleUrls: ['./meus-dados-voluntario.component.scss']
})
export class MeusDadosVoluntarioComponent implements OnInit {
  carregando = false;

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    telefone: [''],
    observacoes: [''],
    restricoesIndisponibilidade: [''],
    novaSenha: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly portalVoluntarioService: PortalVoluntarioService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.portalVoluntarioService.obterMeusDados().subscribe((dados) => {
      this.form.patchValue({
        nome: dados.nome,
        email: dados.email || '',
        telefone: dados.telefone || '',
        observacoes: dados.observacoes || '',
        restricoesIndisponibilidade: dados.restricoesIndisponibilidade || ''
      });
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.carregando = true;
    this.portalVoluntarioService.atualizarMeusDados(this.form.getRawValue() as any).subscribe({
      next: (res) => {
        this.toastr.success(res.mensagem, 'Meus dados');
        this.form.patchValue({ novaSenha: '' });
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem ?? 'Falha ao salvar dados.', 'Atenção');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }
}
