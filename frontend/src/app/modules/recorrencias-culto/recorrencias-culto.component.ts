import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CultoRecorrencia, CultoRecorrenciaDataGeracao, CultoRecorrenciaRequest } from '../../core/models/culto.models';
import { TemplateCulto } from '../../core/models/template-culto.models';
import { CultoService } from '../../core/services/culto.service';
import { TemplateCultoService } from '../../core/services/template-culto.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

@Component({
  selector: 'app-recorrencias-culto',
  templateUrl: './recorrencias-culto.component.html',
  styleUrls: ['./recorrencias-culto.component.scss']
})
export class RecorrenciasCultoComponent implements OnInit {
  recorrencias: CultoRecorrencia[] = [];
  templates: TemplateCulto[] = [];
  recorrenciaEditandoId: number | null = null;
  carregando = false;
  mensagemSucesso = '';
  mensagemErro = '';
  modalGeracaoAberto = false;
  recorrenciaGeracao: CultoRecorrencia | null = null;
  datasGeracao: Array<CultoRecorrenciaDataGeracao & { selecionada: boolean }> = [];
  gerandoCultos = false;

  readonly statusOptions = [
    { id: 1, label: 'Ativo' },
    { id: 2, label: 'Inativo' }
  ];

  readonly diasSemana = [
    { id: 0, label: 'Domingo' },
    { id: 1, label: 'Segunda-feira' },
    { id: 2, label: 'Terça-feira' },
    { id: 3, label: 'Quarta-feira' },
    { id: 4, label: 'Quinta-feira' },
    { id: 5, label: 'Sexta-feira' },
    { id: 6, label: 'Sábado' }
  ];

  readonly recorrenciaForm = this.fb.group({
    nome: ['Culto de Celebração', Validators.required],
    tipoCulto: ['Culto de Celebração', Validators.required],
    diaSemana: [0, Validators.required],
    horarioInicio: ['19:00', Validators.required],
    statusCultoId: [1, Validators.required],
    observacoesGerais: [''],
    templateCultoId: [null as number | null],
    quantidadeSemanasAntecedencia: [12, [Validators.required, Validators.min(1), Validators.max(52)]],
    ativo: [true]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly cultoService: CultoService,
    private readonly templateCultoService: TemplateCultoService
  ) {}

  ngOnInit(): void {
    this.carregarRecorrencias();
    this.carregarTemplates();
  }

  carregarRecorrencias(): void {
    this.cultoService.listarRecorrencias().subscribe((data) => {
      this.recorrencias = data || [];
    });
  }

  carregarTemplates(): void {
    this.templateCultoService.listar().subscribe((data) => {
      this.templates = (data || []).filter((item) => item.ativo);
    });
  }

  async salvarRecorrencia(): Promise<void> {
    if (this.recorrenciaForm.invalid) {
      this.recorrenciaForm.markAllAsTouched();
      return;
    }

    this.mensagemSucesso = '';
    this.mensagemErro = '';
    this.carregando = true;

    const raw = this.recorrenciaForm.getRawValue();
    const dto: CultoRecorrenciaRequest = {
      nome: raw.nome!,
      tipoCulto: raw.tipoCulto!,
      diaSemana: Number(raw.diaSemana),
      horarioInicio: this.normalizarHorario(raw.horarioInicio || ''),
      horarioFimPrevisto: null,
      statusCultoId: Number(raw.statusCultoId),
      observacoesGerais: raw.observacoesGerais || null,
      templateCultoId: raw.templateCultoId ? Number(raw.templateCultoId) : null,
      quantidadeSemanasAntecedencia: Number(raw.quantidadeSemanasAntecedencia || 12),
      ativo: !!raw.ativo
    };

    try {
      if (this.recorrenciaEditandoId === null) {
        await firstValueFrom(this.cultoService.criarRecorrencia(dto));
        this.mensagemSucesso = 'Recorrência criada com sucesso.';
      } else {
        await firstValueFrom(this.cultoService.atualizarRecorrencia(this.recorrenciaEditandoId, dto));
        this.mensagemSucesso = 'Recorrência atualizada com sucesso.';
      }

      this.limparFormulario();
      this.carregarRecorrencias();
    } catch (error) {
      this.mensagemErro = this.extrairMensagemErro(error);
    } finally {
      this.carregando = false;
    }
  }

  editarRecorrencia(recorrencia: CultoRecorrencia): void {
    this.recorrenciaEditandoId = recorrencia.id;
    this.recorrenciaForm.patchValue({
      nome: recorrencia.nome,
      tipoCulto: recorrencia.tipoCulto,
      diaSemana: recorrencia.diaSemana,
      horarioInicio: this.paraHorarioInput(recorrencia.horarioInicio),
      statusCultoId: recorrencia.statusCultoId,
      observacoesGerais: recorrencia.observacoesGerais || '',
      templateCultoId: recorrencia.templateCultoId,
      quantidadeSemanasAntecedencia: recorrencia.quantidadeSemanasAntecedencia,
      ativo: recorrencia.ativo
    });
  }

  async excluirRecorrencia(recorrencia: CultoRecorrencia): Promise<void> {
    const confirmou = await confirmarExclusao(`Deseja excluir a recorrência "${recorrencia.nome}"? Os cultos já gerados serão mantidos.`);
    if (!confirmou) {
      return;
    }

    this.mensagemSucesso = '';
    this.mensagemErro = '';
    this.carregando = true;

    try {
      await firstValueFrom(this.cultoService.excluirRecorrencia(recorrencia.id));
      this.mensagemSucesso = 'Recorrência excluída com sucesso.';
      if (this.recorrenciaEditandoId === recorrencia.id) {
        this.limparFormulario();
      }
      this.carregarRecorrencias();
    } catch (error) {
      this.mensagemErro = this.extrairMensagemErro(error);
    } finally {
      this.carregando = false;
    }
  }

  async gerarCultosRecorrencia(recorrencia: CultoRecorrencia): Promise<void> {
    this.mensagemSucesso = '';
    this.mensagemErro = '';
    this.carregando = true;

    try {
      const resposta = await firstValueFrom(this.cultoService.listarDatasGeracao(recorrencia.id));
      this.recorrenciaGeracao = recorrencia;
      this.datasGeracao = (resposta.datas || []).map((item) => ({
        ...item,
        selecionada: !item.jaExiste
      }));
      this.modalGeracaoAberto = true;
    } catch (error) {
      this.mensagemErro = this.extrairMensagemErro(error);
    } finally {
      this.carregando = false;
    }
  }

  selecionarTodasDatas(): void {
    this.datasGeracao.forEach((item) => {
      if (!item.jaExiste) {
        item.selecionada = true;
      }
    });
  }

  limparSelecaoDatas(): void {
    this.datasGeracao.forEach((item) => {
      if (!item.jaExiste) {
        item.selecionada = false;
      }
    });
  }

  fecharModalGeracao(): void {
    if (this.gerandoCultos) {
      return;
    }

    this.modalGeracaoAberto = false;
    this.recorrenciaGeracao = null;
    this.datasGeracao = [];
  }

  async confirmarGeracao(): Promise<void> {
    if (!this.recorrenciaGeracao) {
      return;
    }

    const datas = this.datasGeracao
      .filter((item) => item.selecionada && !item.jaExiste)
      .map((item) => item.data);
    if (!datas.length) {
      this.mensagemErro = 'Selecione pelo menos uma data para gerar os cultos.';
      return;
    }

    this.mensagemErro = '';
    this.gerandoCultos = true;
    try {
      const resposta = await firstValueFrom(this.cultoService.gerarCultosRecorrencia(
        this.recorrenciaGeracao.id,
        { datas }
      ));
      this.mensagemSucesso = resposta.mensagem;
      this.gerandoCultos = false;
      this.fecharModalGeracao();
      this.carregarRecorrencias();
    } catch (error) {
      this.mensagemErro = this.extrairMensagemErro(error);
    } finally {
      this.gerandoCultos = false;
    }
  }

  formatarDataGeracao(data: string): string {
    const partes = data.split('-');
    if (partes.length !== 3) {
      return data;
    }

    return `${partes[2]}/${partes[1]}/${partes[0]}`;
  }

  diaDataGeracao(data: string): string {
    const partes = data.split('-').map(Number);
    if (partes.length !== 3 || partes.some((parte) => Number.isNaN(parte))) {
      return '';
    }

    return this.diaSemanaLabel(new Date(Date.UTC(partes[0], partes[1] - 1, partes[2])).getUTCDay());
  }

  limparFormulario(): void {
    this.recorrenciaEditandoId = null;
    this.recorrenciaForm.reset({
      nome: 'Culto de Celebração',
      tipoCulto: 'Culto de Celebração',
      diaSemana: 0,
      horarioInicio: '19:00',
      statusCultoId: 1,
      observacoesGerais: '',
      templateCultoId: null,
      quantidadeSemanasAntecedencia: 12,
      ativo: true
    });
  }

  diaSemanaLabel(diaSemana: number): string {
    return this.diasSemana.find((item) => item.id === Number(diaSemana))?.label || '-';
  }

  paraHorarioInput(valor: unknown): string {
    if (!valor) {
      return '';
    }

    if (typeof valor === 'object') {
      const origem = valor as Record<string, unknown>;
      const hora = origem['hours'] ?? origem['hour'] ?? origem['Hours'] ?? origem['Hour'];
      const minuto = origem['minutes'] ?? origem['minute'] ?? origem['Minutes'] ?? origem['Minute'];

      if (typeof hora === 'number' && typeof minuto === 'number') {
        return `${`${hora}`.padStart(2, '0')}:${`${minuto}`.padStart(2, '0')}`;
      }
    }

    const limpo = String(valor).trim();
    const match = limpo.match(/^(\d{1,2}):(\d{2})(?::\d{2})?/);
    if (match) {
      return `${match[1].padStart(2, '0')}:${match[2]}`;
    }

    return limpo.length >= 5 ? limpo.slice(0, 5) : limpo;
  }

  private normalizarHorario(valor: string): string {
    if (!valor) {
      return valor;
    }

    const limpo = valor.trim();
    const match = limpo.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?$/);
    if (match) {
      const hora = match[1].padStart(2, '0');
      const minuto = match[2];
      const segundo = match[3] ?? '00';
      return `${hora}:${minuto}:${segundo}`;
    }

    return limpo.length === 5 ? `${limpo}:00` : limpo;
  }

  private extrairMensagemErro(error: any): string {
    if (error?.error?.mensagem) {
      return error.error.mensagem;
    }

    const errors = error?.error?.errors;
    if (errors && typeof errors === 'object') {
      const primeiraChave = Object.keys(errors)[0];
      if (primeiraChave && Array.isArray(errors[primeiraChave]) && errors[primeiraChave].length > 0) {
        return errors[primeiraChave][0];
      }
    }

    return 'Não foi possível salvar a recorrência.';
  }
}
