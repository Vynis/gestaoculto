import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { Escala } from '../../core/models/escala.models';
import { EscalaService } from '../../core/services/escala.service';
import { Culto } from '../../core/models/culto.models';
import { CultoService } from '../../core/services/culto.service';
import { CadastroService } from '../../core/services/cadastro.service';
import { Ministerio, Voluntario } from '../../core/models/cadastro.models';
import { EscalaModalPrefill } from './escala-modal.component';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

@Component({
  selector: 'app-escalas',
  templateUrl: './escalas.component.html',
  styleUrls: ['./escalas.component.scss']
})
export class EscalasComponent implements OnInit {
  escalas: Escala[] = [];
  cultos: Culto[] = [];
  voluntarios: Voluntario[] = [];
  ministerios: Ministerio[] = [];
  carregandoVoluntarios = false;
  erroVoluntarios: string | null = null;

  modalAberto = false;
  escalaEditando: Escala | null = null;
  prefillModal: EscalaModalPrefill | null = null;
  carregando = false;
  carregouConsulta = false;

  readonly filtro = this.fb.group({
    cultoId: [0, Validators.required],
    ministerioId: [null as number | null]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly escalaService: EscalaService,
    private readonly cultoService: CultoService,
    private readonly cadastroService: CadastroService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.cultoService.listarAtivos().subscribe((data) => {
      this.cultos = data;
      const primeiro = data[0]?.id ?? 0;
      this.filtro.patchValue({ cultoId: primeiro });
      if (primeiro) {
        this.carregar();
      }
    });

    this.cadastroService.listarMinisterios().subscribe((data) => {
      this.ministerios = data || [];
    });

    this.carregarVoluntarios();
  }

  carregar(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.escalas = [];
      this.carregouConsulta = true;
      this.toastr.warning('Selecione um culto para carregar as escalas.', 'Escalas');
      return;
    }

    this.carregando = true;
    this.escalaService.listarPorCulto(cultoId).subscribe((data) => {
      this.escalas = data;
      this.carregando = false;
      this.carregouConsulta = true;
    }, () => {
      this.carregando = false;
      this.carregouConsulta = true;
    });
  }

  abrirModalNovaEscala(prefill?: EscalaModalPrefill): void {
    this.carregarVoluntarios(true);
    this.escalaEditando = null;
    this.prefillModal = prefill || {
      cultoId: Number(this.filtro.value.cultoId) || (this.cultos[0]?.id ?? 0),
      voluntarioId: null,
      ministerioId: null,
      funcao: '',
      observacoes: ''
    };
    this.modalAberto = true;
  }

  editarEscala(escala: Escala): void {
    this.carregarVoluntarios(true);
    this.prefillModal = null;
    this.escalaEditando = escala;
    this.modalAberto = true;
  }

  fecharModalEscala(): void {
    this.modalAberto = false;
    this.escalaEditando = null;
    this.prefillModal = null;
  }

  aoSalvarModalEscala(): void {
    this.fecharModalEscala();
    this.carregar();
  }

  confirmarPresenca(id: number): void {
    this.escalaService.confirmar(id).subscribe(() => {
      this.carregar();
      this.toastr.success('Presença confirmada.', 'Tudo certo');
    });
  }

  cancelarConfirmacao(id: number): void {
    this.escalaService.cancelarConfirmacao(id).subscribe(() => {
      this.carregar();
      this.toastr.success('Confirmação cancelada.', 'Escalas');
    });
  }

  async excluirEscala(escala: Escala): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir a escala "${escala.funcao}"?`);
    if (!confirmou) {
      return;
    }

    this.escalaService.excluir(escala.id).subscribe({
      next: () => {
        this.carregar();
        this.toastr.success('Escala excluída com sucesso.', 'Escalas');
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível excluir escala.', 'Erro');
      }
    });
  }

  async excluirTodasDoCultoSelecionado(): Promise<void> {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.toastr.warning('Selecione um culto antes de excluir.', 'Atenção');
      return;
    }

    const cultoNome = this.cultos.find((x) => x.id === cultoId)?.nome || `#${cultoId}`;
    const confirmou = await confirmarExclusao(`Deseja excluir todas as escalas do culto "${cultoNome}"?`);
    if (!confirmou) {
      return;
    }

    this.escalaService.excluirPorCulto(cultoId).subscribe({
      next: (response) => {
        this.carregar();
        this.toastr.success(response.mensagem, 'Escalas');
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível excluir as escalas do culto.', 'Erro');
      }
    });
  }

  statusTexto(statusId: number): string {
    switch (statusId) {
      case 2:
        return 'Confirmado';
      case 3:
        return 'Ausente';
      case 4:
        return 'Substituído';
      default:
        return 'Pendente';
    }
  }

  statusVisual(statusId: number): 'basic' | 'success' | 'danger' | 'warning' {
    switch (statusId) {
      case 2:
        return 'success';
      case 3:
        return 'danger';
      case 4:
        return 'warning';
      default:
        return 'basic';
    }
  }

  get escalasFiltradas(): Escala[] {
    const ministerioId = Number(this.filtro.value.ministerioId);
    if (!ministerioId) {
      return this.escalas;
    }

    return this.escalas.filter((item) => item.ministerioId === ministerioId);
  }

  get semRegistros(): boolean {
    return this.carregouConsulta && !this.carregando && this.escalasFiltradas.length === 0;
  }

  carregarVoluntarios(silencioso = false): void {
    this.carregandoVoluntarios = true;
    this.erroVoluntarios = null;

    this.cadastroService.listarVoluntarios().subscribe({
      next: (data) => {
        this.voluntarios = (data || [])
          .slice()
          .sort((a, b) => (a.nome || '').localeCompare(b.nome || '', 'pt-BR', { sensitivity: 'base' }));
        this.erroVoluntarios = null;
      },
      error: () => {
        if (!this.voluntarios.length) {
          this.erroVoluntarios = 'Falha ao carregar voluntários.';
        }
        if (!silencioso) {
          this.toastr.danger('Não foi possível carregar a lista de voluntários.', 'Escalas');
        }
        this.carregandoVoluntarios = false;
      },
      complete: () => {
        this.carregandoVoluntarios = false;
      }
    });
  }
}
