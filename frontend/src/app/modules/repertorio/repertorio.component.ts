import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { Culto } from '../../core/models/culto.models';
import { EtapaCulto } from '../../core/models/cronograma.models';
import { Musica } from '../../core/models/musica.models';
import { RepertorioCulto, RepertorioItem } from '../../core/models/repertorio.models';
import { CronogramaService } from '../../core/services/cronograma.service';
import { CultoService } from '../../core/services/culto.service';
import { MusicaService } from '../../core/services/musica.service';
import { RepertorioService } from '../../core/services/repertorio.service';
import { confirmarExclusao } from '../../core/utils/confirm-dialog.util';

@Component({
  selector: 'app-repertorio',
  templateUrl: './repertorio.component.html',
  styleUrls: ['./repertorio.component.scss']
})
export class RepertorioComponent implements OnInit {
  cultos: Culto[] = [];
  musicas: Musica[] = [];
  etapas: EtapaCulto[] = [];
  repertorio: RepertorioCulto = { cultoId: 0, observacoes: null, itens: [] };

  readonly filtro = this.fb.group({
    cultoId: [0, Validators.required]
  });

  readonly itemForm = this.fb.group({
    musicaId: [0, Validators.required],
    etapaCultoId: [null as number | null],
    responsavel: [''],
    observacoes: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly cultoService: CultoService,
    private readonly cronogramaService: CronogramaService,
    private readonly musicaService: MusicaService,
    private readonly repertorioService: RepertorioService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.filtro.controls.cultoId.valueChanges.subscribe((valor) => {
      const cultoId = Number(valor || 0);
      this.etapas = [];
      this.itemForm.patchValue({ etapaCultoId: null });

      if (!cultoId) {
        return;
      }

      this.carregarEtapasDoCulto(cultoId);
    });

    this.cultoService.listar().subscribe((data) => {
      this.cultos = data;
    });

    this.musicaService.listar(undefined, true).subscribe((data) => {
      this.musicas = data;
    });
  }

  carregar(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.etapas = [];
      return;
    }

    this.carregarEtapasDoCulto(cultoId);

    this.repertorioService.obterPorCulto(cultoId).subscribe((data) => {
      this.repertorio = {
        ...data,
        cultoId,
        itens: (data.itens || []).slice().sort((a, b) => a.ordem - b.ordem)
      };
    });
  }

  adicionarItem(): void {
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const raw = this.itemForm.getRawValue();
    const musica = this.musicas.find((x) => x.id === Number(raw.musicaId));
    const etapa = this.etapas.find((x) => x.id === Number(raw.etapaCultoId));

    const item: RepertorioItem = {
      musicaId: Number(raw.musicaId),
      musicaTitulo: musica?.titulo,
      etapaCultoId: raw.etapaCultoId,
      etapaAtividade: etapa?.atividade,
      ordem: this.repertorio.itens.length + 1,
      responsavel: raw.responsavel || null,
      observacoes: raw.observacoes || null
    };

    this.repertorio.itens = [...this.repertorio.itens, item];
    this.itemForm.reset({ musicaId: 0, etapaCultoId: null, responsavel: '', observacoes: '' });
  }

  async removerItem(index: number): Promise<void> {
    const item = this.repertorio.itens[index];
    const nome = item?.musicaTitulo || `Música #${item?.musicaId || index + 1}`;
    const confirmou = await confirmarExclusao(`Deseja excluir o item "${nome}" do repertório?`);
    if (!confirmou) {
      return;
    }

    this.repertorio.itens.splice(index, 1);
    this.repertorio.itens = this.repertorio.itens.map((x, i) => ({ ...x, ordem: i + 1 }));
  }

  salvar(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      this.toastr.warning('Selecione um culto.', 'Repertório');
      return;
    }

    this.repertorioService.salvar({
      cultoId,
      observacoes: this.repertorio.observacoes || null,
      itens: this.repertorio.itens.map((item, index) => ({
        ...item,
        ordem: index + 1
      }))
    }).subscribe(() => {
      this.toastr.success('Repertório salvo com sucesso.', 'Repertório');
      this.carregar();
    });
  }

  duplicarCultoAnterior(): void {
    const cultoId = Number(this.filtro.value.cultoId);
    if (!cultoId) {
      return;
    }

    this.repertorioService.duplicarDoCultoAnterior(cultoId).subscribe({
      next: () => {
        this.toastr.success('Repertório duplicado do culto anterior.', 'Repertório');
        this.carregar();
      },
      error: (error) => {
        this.toastr.warning(error?.error?.mensagem || 'Não foi possível duplicar repertório.', 'Repertório');
      }
    });
  }

  private carregarEtapasDoCulto(cultoId: number): void {
    this.cronogramaService.listarPorCulto(cultoId).subscribe((etapas) => {
      const etapasUnicasPorAssinatura = new Map<string, EtapaCulto>();

      (etapas || [])
        .filter((item) => item.cultoId === cultoId)
        .filter((item, index, arr) => arr.findIndex((x) => x.id === item.id) === index)
        .sort((a, b) => a.sequencia - b.sequencia || a.id - b.id)
        .forEach((item) => {
          const chave = this.assinaturaEtapa(item);
          if (!etapasUnicasPorAssinatura.has(chave)) {
            etapasUnicasPorAssinatura.set(chave, item);
          }
        });

      this.etapas = Array.from(etapasUnicasPorAssinatura.values())
        .sort((a, b) => a.sequencia - b.sequencia || a.id - b.id);

      const etapaSelecionada = Number(this.itemForm.value.etapaCultoId || 0);
      if (etapaSelecionada && !this.etapas.some((item) => item.id === etapaSelecionada)) {
        this.itemForm.patchValue({ etapaCultoId: null });
      }
    });
  }

  private assinaturaEtapa(item: EtapaCulto): string {
    const sequencia = Number(item.sequencia || 0);
    const atividade = String(item.atividade || '').trim().toLowerCase();
    return `${sequencia}|${atividade}`;
  }
}
