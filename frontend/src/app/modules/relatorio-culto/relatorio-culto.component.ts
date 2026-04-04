import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { Router } from '@angular/router';
import { Culto } from '../../core/models/culto.models';
import { RelatorioCultoDiaResponse, RelatorioCultoRepertorioItem, RelatorioCultoVoluntario } from '../../core/models/relatorio-culto.models';
import { RelatorioService } from '../../core/services/relatorio.service';

@Component({
  selector: 'app-relatorio-culto',
  templateUrl: './relatorio-culto.component.html',
  styleUrls: ['./relatorio-culto.component.scss']
})
export class RelatorioCultoComponent implements OnInit {
  carregando = false;
  relatorio: RelatorioCultoDiaResponse | null = null;
  cultos: Culto[] = [];
  modoPublico = false;

  readonly filtro = this.fb.group({
    cultoId: [null as number | null, Validators.required]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly relatorioService: RelatorioService,
    private readonly toastr: NbToastrService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.modoPublico = this.router.url.includes('relatorio-culto-publico');

    this.relatorioService.listarCultosParaRelatorio().subscribe((data) => {
      this.cultos = data.slice().sort((a, b) => this.compararCultosDesc(a, b));
      const primeiro = this.cultos[0]?.id ?? null;
      this.filtro.patchValue({ cultoId: primeiro });
    });
  }

  gerar(): void {
    if (this.filtro.invalid) {
      this.filtro.markAllAsTouched();
      return;
    }

    const cultoId = Number(this.filtro.value.cultoId || 0);
    if (!cultoId) {
      this.toastr.warning('Selecione um culto para gerar o relatório.', 'Relatorio');
      return;
    }

    this.carregando = true;

    this.relatorioService.obterRelatorioCultoPorId(cultoId).subscribe({
      next: (response) => {
        this.relatorio = response;
        if (!response.cultos.length) {
          this.toastr.warning('Nenhum culto encontrado para a data selecionada.', 'Relatorio');
        }
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Nao foi possivel gerar o relatorio.', 'Erro');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  percentualConfirmacao(total: number, confirmados: number): string {
    if (!total) {
      return '0%';
    }

    const valor = Math.round((confirmados / total) * 100);
    return `${valor}%`;
  }

  async imprimir(): Promise<void> {
    if (!this.relatorio?.cultos?.length) {
      this.toastr.warning('Gere o relatório antes de imprimir.', 'Relatorio');
      return;
    }

    const pdfMake = await this.carregarPdfMake();
    pdfMake.createPdf(this.montarDocumentoPdf()).open();
  }

  async gerarPdf(): Promise<void> {
    if (!this.relatorio?.cultos?.length) {
      this.toastr.warning('Gere o relatório antes de exportar.', 'Relatorio');
      return;
    }

    const cultoNome = this.relatorio.cultos[0]?.culto?.nome || 'culto';
    const nomeArquivo = `relatorio-${this.normalizarNomeArquivo(cultoNome)}.pdf`;
    const pdfMake = await this.carregarPdfMake();
    pdfMake.createPdf(this.montarDocumentoPdf()).download(nomeArquivo);
  }

  horaTexto(valor: any): string {
    if (!valor) {
      return '--:--';
    }

    if (typeof valor === 'object') {
      const horas = Number(valor.hours ?? valor.Hours ?? valor.hour ?? valor.Hour);
      const minutos = Number(valor.minutes ?? valor.Minutes ?? valor.minute ?? valor.Minute);

      if (!Number.isNaN(horas) && !Number.isNaN(minutos)) {
        return `${`${horas}`.padStart(2, '0')}:${`${minutos}`.padStart(2, '0')}`;
      }

      const totalSeconds = Number(valor.totalSeconds ?? valor.TotalSeconds);
      if (!Number.isNaN(totalSeconds)) {
        const h = Math.floor(totalSeconds / 3600) % 24;
        const m = Math.floor((totalSeconds % 3600) / 60);
        return `${`${h}`.padStart(2, '0')}:${`${m}`.padStart(2, '0')}`;
      }

      const ticks = Number(valor.ticks ?? valor.Ticks);
      if (!Number.isNaN(ticks)) {
        const totalMin = Math.floor(ticks / 10000000 / 60);
        const h = Math.floor(totalMin / 60) % 24;
        const m = totalMin % 60;
        return `${`${h}`.padStart(2, '0')}:${`${m}`.padStart(2, '0')}`;
      }
    }

    const texto = String(valor).trim();
    const match = texto.match(/(\d{1,2}):(\d{2})/);
    if (match) {
      return `${match[1].padStart(2, '0')}:${match[2]}`;
    }

    const data = new Date(texto);
    if (Number.isNaN(data.getTime())) {
      return texto;
    }

    const hora = `${data.getHours()}`.padStart(2, '0');
    const minuto = `${data.getMinutes()}`.padStart(2, '0');
    return `${hora}:${minuto}`;
  }

  voluntariosAgrupadosPorEquipe(voluntarios: RelatorioCultoVoluntario[]): { equipe: string; itens: RelatorioCultoVoluntario[] }[] {
    const mapa = new Map<string, RelatorioCultoVoluntario[]>();

    for (const item of voluntarios || []) {
      const equipe = (item.ministerioNome || 'Sem equipe').trim();
      const atual = mapa.get(equipe) || [];
      atual.push(item);
      mapa.set(equipe, atual);
    }

    return Array.from(mapa.entries())
      .sort((a, b) => a[0].localeCompare(b[0], 'pt-BR'))
      .map(([equipe, itens]) => ({
        equipe,
        itens: itens.slice().sort((a, b) => (a.voluntarioNome || '').localeCompare(b.voluntarioNome || '', 'pt-BR'))
      }));
  }

  tomRepertorio(item: RelatorioCultoRepertorioItem): string | null {
    const valor = item.musicaTom ?? item.tom;
    return this.textoValido(valor);
  }

  linkCifraRepertorio(item: RelatorioCultoRepertorioItem): string | null {
    return this.textoValido(item.musicaLinkCifra ?? item.linkCifra);
  }

  linkVideoRepertorio(item: RelatorioCultoRepertorioItem): string | null {
    return this.textoValido(item.musicaLinkVideo ?? item.linkVideo);
  }

  observacoesItemRepertorio(item: RelatorioCultoRepertorioItem): string | null {
    return this.textoValido(item.observacoes);
  }

  observacoesMusicaRepertorio(item: RelatorioCultoRepertorioItem): string | null {
    return this.textoValido(item.musicaObservacoes);
  }

  private compararCultosDesc(a: Culto, b: Culto): number {
    const dataA = `${a.dataCulto || ''}T${this.horaTexto(a.horarioInicio)}:00`;
    const dataB = `${b.dataCulto || ''}T${this.horaTexto(b.horarioInicio)}:00`;
    const valorA = new Date(dataA).getTime();
    const valorB = new Date(dataB).getTime();
    return valorB - valorA;
  }

  private montarDocumentoPdf(): any {
    const conteudo: any[] = [];

    conteudo.push(
      { text: 'Relatorio de Culto', style: 'titulo' },
      { text: `Gerado em ${new Date().toLocaleString('pt-BR')}`, style: 'subtitulo' }
    );

    for (const bloco of this.relatorio?.cultos || []) {
      const culto = bloco.culto;
      const voluntariosAgrupados = this.voluntariosAgrupadosPorEquipe(bloco.voluntarios || []);

      conteudo.push(
        { text: culto.nome, style: 'blocoTitulo', margin: [0, 12, 0, 2] },
        {
          text: `${culto.tipoCulto} - Inicio ${this.horaTexto(culto.horarioInicio)}${culto.horarioFimPrevisto ? ` - Fim previsto ${this.horaTexto(culto.horarioFimPrevisto)}` : ''}`,
          style: 'texto'
        },
        { text: `Status: ${culto.statusCultoNome || `Status #${culto.statusCultoId}`}`, style: 'texto' },
        {
          text: `Resumo: Etapas ${bloco.resumo.totalEtapas} | Escalados ${bloco.resumo.totalEscalados} | Confirmados ${bloco.resumo.totalConfirmados} | Pendentes ${bloco.resumo.totalPendentes}`,
          style: 'texto',
          margin: [0, 0, 0, 6]
        }
      );

      conteudo.push({ text: 'Cronograma', style: 'secao' });
      for (const etapa of bloco.cronograma || []) {
        conteudo.push({ text: `#${etapa.sequencia} ${etapa.atividade} - ${this.horaTexto(etapa.horarioInicio)} (${etapa.duracaoMinutos} min)`, style: 'item' });
        if (etapa.descricao) {
          conteudo.push({ text: `Descricao: ${etapa.descricao}`, style: 'textoPequeno' });
        }
        for (const acao of etapa.acoesMinisterio || []) {
          conteudo.push({
            text: `- ${acao.ministerioNome || `Ministerio #${acao.ministerioId}`}: ${acao.descricaoAcao}${acao.observacao ? ` (${acao.observacao})` : ''}`,
            style: 'textoPequeno',
            margin: [8, 0, 0, 0]
          });
        }
      }

      conteudo.push({ text: 'Repertorio de musicas', style: 'secao', margin: [0, 8, 0, 2] });
      const itensRepertorio = bloco.repertorio?.itens || [];
      if (!itensRepertorio.length) {
        conteudo.push({ text: '- Sem repertorio cadastrado para este culto.', style: 'textoPequeno' });
      } else {
        for (const item of itensRepertorio) {
          const detalhes: string[] = [];
          if (item.etapaAtividade) {
            detalhes.push(`Etapa: ${item.etapaAtividade}`);
          }
          if (item.responsavel) {
            detalhes.push(`Responsavel: ${item.responsavel}`);
          }

          const tom = this.tomRepertorio(item);
          if (tom) {
            detalhes.push(`Tom: ${tom}`);
          }

          const linkCifra = this.linkCifraRepertorio(item);
          if (linkCifra) {
            detalhes.push(`Link cifra: ${linkCifra}`);
          }

          const linkVideo = this.linkVideoRepertorio(item);
          if (linkVideo) {
            detalhes.push(`Link video: ${linkVideo}`);
          }

          const observacaoItem = this.observacoesItemRepertorio(item);
          if (observacaoItem) {
            detalhes.push(`Obs repertorio: ${observacaoItem}`);
          }

          const observacaoMusica = this.observacoesMusicaRepertorio(item);
          if (observacaoMusica) {
            detalhes.push(`Obs musica: ${observacaoMusica}`);
          }

          conteudo.push({
            text: `- ${item.ordem}. ${item.musicaTitulo || `Musica #${item.musicaId}`}${detalhes.length ? ` | ${detalhes.join(' | ')}` : ''}`,
            style: 'textoPequeno'
          });
        }
      }

      conteudo.push({ text: 'Voluntarios por equipe', style: 'secao', margin: [0, 8, 0, 2] });
      for (const grupo of voluntariosAgrupados) {
        conteudo.push({ text: grupo.equipe, style: 'item' });
        for (const item of grupo.itens) {
          conteudo.push({
            text: `- ${item.voluntarioNome || `Voluntario #${item.voluntarioId}`} | Funcao: ${item.funcao} | Status: ${item.presencaStatusNome || `Status #${item.presencaStatusId}`}`,
            style: 'textoPequeno',
            margin: [8, 0, 0, 0]
          });
        }
      }
    }

    return {
      pageMargins: [32, 28, 32, 30],
      content: conteudo,
      styles: {
        titulo: { fontSize: 18, bold: true },
        subtitulo: { fontSize: 10, color: '#4b5563', margin: [0, 2, 0, 10] },
        blocoTitulo: { fontSize: 13, bold: true },
        secao: { fontSize: 11, bold: true, margin: [0, 6, 0, 3] },
        item: { fontSize: 10, bold: true, margin: [0, 2, 0, 1] },
        texto: { fontSize: 10, margin: [0, 0, 0, 1] },
        textoPequeno: { fontSize: 9, margin: [0, 0, 0, 1] }
      },
      defaultStyle: {
        fontSize: 10
      }
    };
  }

  private normalizarNomeArquivo(nome: string): string {
    return (nome || 'culto')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  private textoValido(valor: string | null | undefined): string | null {
    const texto = String(valor || '').trim();
    return texto ? texto : null;
  }

  private async carregarPdfMake(): Promise<any> {
    const pdfMakeModule = await import('pdfmake/build/pdfmake');
    const pdfFontsModule = await import('pdfmake/build/vfs_fonts');
    const instancia = (pdfMakeModule as any).default || pdfMakeModule;
    const fontes = (pdfFontsModule as any).default || pdfFontsModule;
    const vfs = (fontes as any).pdfMake?.vfs || fontes;

    if (typeof instancia.addVirtualFileSystem === 'function') {
      instancia.addVirtualFileSystem(vfs);
    } else {
      instancia.vfs = vfs;
    }

    return instancia;
  }

}
