import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { ActivatedRoute, Router } from '@angular/router';
import { Culto } from '../../core/models/culto.models';
import { MinisterioCompleto } from '../../core/models/ministerio.models';
import { RelatorioCultoDiaResponse, RelatorioCultoEtapa, RelatorioCultoRepertorioItem } from '../../core/models/relatorio-culto.models';
import { MinisterioService } from '../../core/services/ministerio.service';
import { CultoService } from '../../core/services/culto.service';
import { RelatorioService } from '../../core/services/relatorio.service';

type ColunaOperacional = 'pulpito' | 'iluminacao' | 'telao' | 'som' | 'atmosfera';

interface LinhaCronogramaModelo {
  horario: string;
  duracao: string;
  atividade: string;
  pulpitoMicrofone: string;
  iluminacaoSalao: string;
  telao: string;
  som: string;
  atmosfera: string;
  observacao: string;
}

interface LinhaLideresEquipe {
  equipe: string;
  lideres: string;
}

interface LinhaRepertorio {
  ordem: number;
  etapa: string;
  musica: string;
  tom: string;
  responsavel: string;
  observacoes: string;
}

interface LinhaCronogramaAdicional {
  horario: string;
  atividade: string;
  voluntarios: string[];
}

interface SecaoCronogramaAdicional {
  titulo: string;
  quantidadeVoluntarios: number;
  linhas: LinhaCronogramaAdicional[];
}

interface SecaoVoluntariosMinisterio {
  titulo: string;
  voluntarios: Array<{ nome: string; funcao: string }>;
}

interface SecaoMenorPreview {
  titulo: string;
  tipo: 'adicional' | 'ministerio' | 'lideres' | 'repertorio';
  variante: number;
  cabecalho: string[];
  linhas: string[][];
}

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
  mensagemPublica = '';
  ministeriosAtivos: MinisterioCompleto[] = [];

  linhasCronograma: LinhaCronogramaModelo[] = [];
  cronogramasAdicionais: SecaoCronogramaAdicional[] = [];
  secoesVoluntariosMinisterio: SecaoVoluntariosMinisterio[] = [];
  linhasLideresEquipe: LinhaLideresEquipe[] = [];
  linhasRepertorio: LinhaRepertorio[] = [];
  private logoPdfDataUrl: string | null = null;

  readonly filtro = this.fb.group({
    cultoId: [null as number | null, Validators.required],
    visualizacao: ['tabela' as 'tabela' | 'cards']
  });

  get visualizacaoSelecionada(): 'tabela' | 'cards' {
    return (this.filtro.value.visualizacao as 'tabela' | 'cards') || 'tabela';
  }

  get ehVisualizacaoTabela(): boolean {
    return this.visualizacaoSelecionada === 'tabela';
  }

  get ehVisualizacaoCards(): boolean {
    return this.visualizacaoSelecionada === 'cards';
  }

  get textoBotaoPdf(): string {
    return this.ehVisualizacaoCards ? 'Exportar PDF em cards' : 'Exportar PDF em tabela';
  }

  get temDadosRelatorio(): boolean {
    return !!(
      this.linhasCronograma.length ||
      this.cronogramasAdicionais.length ||
      this.secoesVoluntariosMinisterio.length ||
      this.linhasLideresEquipe.length ||
      this.linhasRepertorio.length
    );
  }

  get intervaloCronograma(): string {
    if (!this.linhasCronograma.length) {
      return '--:--';
    }

    const inicio = this.linhasCronograma[0]?.horario || '--:--';
    const fim = this.linhasCronograma[this.linhasCronograma.length - 1]?.horario || '--:--';
    return `${inicio} - ${fim}`;
  }

  get cultoNomeCards(): string {
    return this.relatorio?.cultos?.[0]?.culto?.nome || 'Culto';
  }

  categoriaLinhaClasse(linha: LinhaCronogramaModelo): string {
    const texto = `${linha.atividade} ${linha.observacao}`.toLowerCase();
    if (texto.includes('encerr')) {
      return 'cat-fim';
    }
    if (texto.includes('palavra') || texto.includes('prega')) {
      return 'cat-pal';
    }
    if (texto.includes('louvor') || texto.includes('music') || texto.includes('repert')) {
      return 'cat-lou';
    }
    if (texto.includes('oferta') || texto.includes('gratidao') || texto.includes('especial') || texto.includes('video')) {
      return 'cat-esp';
    }
    if (texto.includes('entrada') || texto.includes('fila') || texto.includes('recepc')) {
      return 'cat-ent';
    }
    return 'cat-org';
  }

  constructor(
    private readonly fb: FormBuilder,
    private readonly relatorioService: RelatorioService,
    private readonly ministerioService: MinisterioService,
    private readonly toastr: NbToastrService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.modoPublico = this.router.url.includes('relatorio-culto-publico');

    if (this.modoPublico) {
      this.filtro.patchValue({ visualizacao: 'cards' });
      this.route.queryParamMap.subscribe((params) => {
        const token = String(params.get('t') || '').trim();
        if (!token) {
          this.mensagemPublica = 'O link do relatório é inválido ou está incompleto.';
          return;
        }
        this.gerarPorToken(token);
      });
      return;
    }

    this.relatorioService.listarCultosParaRelatorio().subscribe((data) => {
      this.cultos = CultoService.ordenarPorProximidade(data);
      const primeiro = this.cultos[0]?.id ?? null;
      this.filtro.patchValue({ cultoId: primeiro });
    });

    this.ministerioService.listar().subscribe({
      next: (data) => {
        this.ministeriosAtivos = (data || []).filter((item) => !!item?.ativo);
      },
      error: () => {
        this.ministeriosAtivos = [];
      }
    });
  }

  gerar(): void {
    if (this.filtro.invalid) {
      this.filtro.markAllAsTouched();
      return;
    }

    const cultoId = Number(this.filtro.value.cultoId || 0);
    if (!cultoId) {
      this.toastr.warning('Selecione um culto para gerar o relatório.', 'Relatório');
      return;
    }

    this.carregando = true;
    this.relatorioService.obterRelatorioCultoPorId(cultoId).subscribe({
      next: (response) => {
        this.relatorio = response;
        this.montarModeloVisual(response);
        if (!response.cultos.length) {
          this.toastr.warning('Nenhum culto encontrado para o filtro selecionado.', 'Relatório');
        }
      },
      error: (error) => {
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível gerar o relatório.', 'Erro');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  private gerarPorToken(token: string): void {
    this.carregando = true;
    this.mensagemPublica = '';
    this.relatorioService.obterRelatorioCompartilhado(token).subscribe({
      next: (response) => {
        this.relatorio = response;
        this.montarModeloVisual(response);
        if (!response.cultos.length) {
          this.mensagemPublica = 'O relatório deste culto não está disponível.';
        }
      },
      error: (error) => {
        this.relatorio = null;
        this.mensagemPublica = error?.error?.mensagem || 'O link do relatório é inválido ou expirou.';
        this.carregando = false;
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  async exportarXlsx(): Promise<void> {
    if (!this.linhasCronograma.length) {
      this.toastr.warning('Gere o relatório antes de exportar.', 'Relatório');
      return;
    }

    const modulo = await import('exceljs');
    const ExcelJS = (modulo as any).default || modulo;
    const workbook = new ExcelJS.Workbook();
    const sheet = workbook.addWorksheet('RelatorioCulto');
    const secoesMenores = this.montarSecoesMenoresPreview();
    const larguraBlocoMenor = Math.max(6, ...secoesMenores.map((secao) => secao.cabecalho.length || 1));
    const colunaInicioDireita = larguraBlocoMenor + 2;
    const totalColunasPlanilha = Math.max(9, (larguraBlocoMenor * 2) + 1);
    const ultimaColunaPlanilha = this.colunaExcel(totalColunasPlanilha);

    const titulo = 'CRONOGRAMA DE CULTO';
    const data = this.dataTitulo;

    sheet.mergeCells(`A1:${ultimaColunaPlanilha}1`);
    sheet.getCell('A1').value = titulo;
    sheet.getCell('A1').font = { bold: true, size: 13 };
    sheet.getCell('A1').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFFF00' } };
    sheet.getCell('A1').alignment = { horizontal: 'left', vertical: 'middle' };

    sheet.mergeCells(`A2:${ultimaColunaPlanilha}2`);
    sheet.getCell('A2').value = `DATA: ${data}`;
    sheet.getCell('A2').font = { bold: true, size: 11 };
    sheet.getCell('A2').alignment = { horizontal: 'left', vertical: 'middle' };

    const cabecalho = ['HORÁRIO', 'DURAÇÃO', 'ATIVIDADE', 'PÚLPITO / MICROFONE', 'ILUMINAÇÃO SALÃO', 'TELÃO', 'SOM', 'ATMOSFERA', 'OBSERVAÇÃO'];
    sheet.addRow([]);
    const rowHeader = sheet.addRow(cabecalho);
    rowHeader.eachCell((cell: any) => {
      cell.font = { bold: true, size: 10 };
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFFF00' } };
      cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
      cell.border = this.bordaPadrao();
    });

    for (const linha of this.linhasCronograma) {
      const row = sheet.addRow([
        linha.horario,
        linha.duracao,
        linha.atividade,
        linha.pulpitoMicrofone,
        linha.iluminacaoSalao,
        linha.telao,
        linha.som,
        linha.atmosfera,
        linha.observacao
      ]);

      row.eachCell((cell: any, col: number) => {
        cell.border = this.bordaPadrao();
        cell.alignment = { vertical: 'top', horizontal: col <= 2 ? 'center' : 'left', wrapText: true };
      });
    }

    let linhaCursor = sheet.rowCount + 2;
    for (let index = 0; index < secoesMenores.length; index += 2) {
      const esquerda = secoesMenores[index];
      const direita = secoesMenores[index + 1];

      const fimEsquerda = this.escreverSecaoMenorXlsx(sheet, linhaCursor, 1, larguraBlocoMenor, esquerda);
      const fimDireita = direita
        ? this.escreverSecaoMenorXlsx(sheet, linhaCursor, colunaInicioDireita, larguraBlocoMenor, direita)
        : linhaCursor;

      linhaCursor = Math.max(fimEsquerda, fimDireita) + 1;
    }

    const largurasBase = [10, 10, 34, 24, 22, 18, 22, 24, 34];
    sheet.columns = Array.from({ length: totalColunasPlanilha }, (_, index) => ({
      width: largurasBase[index] ?? 20
    }));

    if (colunaInicioDireita > 1) {
      sheet.getColumn(colunaInicioDireita - 1).width = 4;
    }

    const buffer = await workbook.xlsx.writeBuffer();
    const cultoNome = this.relatorio?.cultos?.[0]?.culto?.nome || 'culto';
    this.baixarArquivo(buffer, `cronograma-${this.normalizarNomeArquivo(cultoNome)}.xlsx`, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');
  }

  async gerarPdf(): Promise<void> {
    if (!this.linhasCronograma.length) {
      this.toastr.warning('Gere o relatório antes de exportar.', 'Relatório');
      return;
    }

    const pdfMake = await this.carregarPdfMake();
    const logoDataUrl = await this.carregarLogoPdfDataUrl();
    const dataArquivo = this.dataCultoParaNomeArquivo();

    if (this.ehVisualizacaoCards) {
      pdfMake.createPdf(this.montarDocumentoPdfCards(logoDataUrl)).download(`RELATORIO_CULTO_CARDS_${dataArquivo}.pdf`);
      return;
    }

    pdfMake.createPdf(this.montarDocumentoPdf(logoDataUrl)).download(`CRONOGRAMA_CULTO_${dataArquivo}.pdf`);
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

    return `${`${data.getHours()}`.padStart(2, '0')}:${`${data.getMinutes()}`.padStart(2, '0')}`;
  }

  get dataTitulo(): string {
    const dataBase = this.relatorio?.cultos?.[0]?.culto?.dataCulto || this.relatorio?.data;
    return this.formatarData(dataBase);
  }

  get tituloCulto(): string {
    const nome = this.relatorio?.cultos?.[0]?.culto?.nome || 'Culto';
    return `${nome} - ${this.dataTitulo}`;
  }

  private montarModeloVisual(relatorio: RelatorioCultoDiaResponse): void {
    this.linhasCronograma = [];
    this.cronogramasAdicionais = [];
    this.secoesVoluntariosMinisterio = [];
    this.linhasLideresEquipe = [];
    this.linhasRepertorio = [];

    const mapaBlocosAdicionais = new Map<string, LinhaCronogramaAdicional[]>();
    const mapaVoluntariosPorMinisterio = new Map<number, { nome: string; voluntarios: Map<string, Set<string>> }>();

    for (const bloco of relatorio.cultos || []) {
      const escalas = bloco.voluntarios || [];
      const repertorio = bloco.repertorio?.itens || [];

      const etapasOrdenadas = (bloco.cronograma || [])
        .slice()
        .sort((a, b) => Number(a.sequencia || 0) - Number(b.sequencia || 0));

      const etapasPrincipais = etapasOrdenadas.filter((etapa) => this.normalizarBloco(etapa.blocoCronograma) === 'PRINCIPAL');
      const etapasAdicionais = etapasOrdenadas.filter((etapa) => this.normalizarBloco(etapa.blocoCronograma) !== 'PRINCIPAL');

      for (const etapa of etapasPrincipais) {
        const escalasEtapa = escalas.filter((item) => Number(item.etapaCultoId || 0) === Number(etapa.id));
        const repertorioEtapa = repertorio.filter((item) => Number(item.etapaCultoId || 0) === Number(etapa.id));

        const colunas = this.montarColunasOperacionais(etapa, escalasEtapa);
        const observacoes = this.juntarObservacoes(
          etapa.descricao,
          ...escalasEtapa.map((item) => item.observacoes)
        );

        const atividade = this.montarAtividadeEtapa(etapa, repertorioEtapa);

        this.linhasCronograma.push({
          horario: this.montarFaixaHorario(etapa),
          duracao: `${Math.max(1, Number(etapa.duracaoMinutos || 0))} MIN`,
          atividade,
          pulpitoMicrofone: colunas.pulpito.join('\n') || '-',
          iluminacaoSalao: colunas.iluminacao.join('\n') || '-',
          telao: colunas.telao.join('\n') || '-',
          som: colunas.som.join('\n') || '-',
          atmosfera: colunas.atmosfera.join('\n') || '-',
          observacao: observacoes
        });

      }

      for (const etapa of etapasAdicionais) {
        const nomeBloco = this.normalizarBloco(etapa.blocoCronograma);
        const escalasEtapa = escalas
          .filter((item) => Number(item.etapaCultoId || 0) === Number(etapa.id))
          .slice()
          .sort((a, b) => String(a.voluntarioNome || '').localeCompare(String(b.voluntarioNome || ''), 'pt-BR'));

        const voluntarios = escalasEtapa
          .map((item) => this.textoValido(item.voluntarioNome) || `Voluntário #${item.voluntarioId}`)
          .filter((item) => !!item);

        if (!mapaBlocosAdicionais.has(nomeBloco)) {
          mapaBlocosAdicionais.set(nomeBloco, []);
        }

        mapaBlocosAdicionais.get(nomeBloco)!.push({
          horario: this.montarFaixaHorario(etapa),
          atividade: this.textoValido(etapa.atividade) || 'Etapa',
          voluntarios: voluntarios.length ? voluntarios : ['*']
        });
      }

      for (const escala of escalas) {
        const ministerioId = Number(escala.ministerioId || 0);
        if (!ministerioId) {
          continue;
        }

        const ministerioNome = this.textoValido(escala.ministerioNome) || `Ministério #${ministerioId}`;
        const voluntarioNome = this.textoValido(escala.voluntarioNome) || `Voluntário #${escala.voluntarioId}`;
        const funcaoNome = this.textoValido(escala.funcao) || '-';

        if (!mapaVoluntariosPorMinisterio.has(ministerioId)) {
          mapaVoluntariosPorMinisterio.set(ministerioId, {
            nome: ministerioNome,
            voluntarios: new Map<string, Set<string>>()
          });
        }

        const itemMinisterio = mapaVoluntariosPorMinisterio.get(ministerioId)!;
        if (!itemMinisterio.voluntarios.has(voluntarioNome)) {
          itemMinisterio.voluntarios.set(voluntarioNome, new Set<string>());
        }
        itemMinisterio.voluntarios.get(voluntarioNome)!.add(funcaoNome);
      }

      this.linhasRepertorio = this.linhasRepertorio.concat(this.montarLinhasRepertorio(repertorio, bloco.cronograma || []));
    }

    this.linhasLideresEquipe = this.ministeriosAtivos
      .slice()
      .sort((a, b) => String(a.nome || '').localeCompare(String(b.nome || ''), 'pt-BR'))
      .map((ministerio) => {
        const lideres = (ministerio.lideres || [])
          .slice()
          .sort((a, b) => {
            if (a.principal !== b.principal) {
              return a.principal ? -1 : 1;
            }
            return String(a.usuarioNome || '').localeCompare(String(b.usuarioNome || ''), 'pt-BR');
          })
          .map((item) => String(item.usuarioNome || '').trim())
          .filter((nome) => !!nome);

        return {
          equipe: this.textoValido(ministerio.nome) || 'Sem equipe',
          lideres: lideres.length ? lideres.join('\n') : 'Sem líder cadastrado'
        };
      });

    this.cronogramasAdicionais = Array.from(mapaBlocosAdicionais.entries())
      .sort((a, b) => a[0].localeCompare(b[0], 'pt-BR'))
      .map(([nomeBloco, linhas]) => {
        const quantidadeVoluntarios = Math.max(2, ...linhas.map((linha) => linha.voluntarios.length));
        const linhasNormalizadas = linhas
          .slice()
          .sort((a, b) => this.compararHorarioTexto(a.horario, b.horario))
          .map((linha) => ({
            ...linha,
            voluntarios: [
              ...linha.voluntarios,
              ...Array.from({ length: Math.max(0, quantidadeVoluntarios - linha.voluntarios.length) }, () => '*')
            ]
          }));

        return {
          titulo: `Cronograma - ${nomeBloco}`,
          quantidadeVoluntarios,
          linhas: linhasNormalizadas
        };
      });

    this.secoesVoluntariosMinisterio = Array.from(mapaVoluntariosPorMinisterio.values())
      .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'))
      .map((item) => ({
        titulo: `Ministério - ${item.nome}`,
        voluntarios: Array.from(item.voluntarios.entries())
          .sort((a, b) => a[0].localeCompare(b[0], 'pt-BR'))
          .map(([nome, funcoes]) => ({
            nome,
            funcao: Array.from(funcoes).sort((a, b) => a.localeCompare(b, 'pt-BR')).join(', ')
          }))
      }));
  }

  private montarAtividadeEtapa(etapa: RelatorioCultoEtapa, repertorioEtapa: RelatorioCultoRepertorioItem[]): string {
    const atividadeBase = this.textoValido(etapa.atividade) || 'Etapa';
    if (!repertorioEtapa.length) {
      return atividadeBase;
    }

    const musicas = repertorioEtapa
      .slice()
      .sort((a, b) => Number(a.ordem || 0) - Number(b.ordem || 0))
      .map((item) => `${item.ordem}. ${item.musicaTitulo || `Música #${item.musicaId}`}`)
      .join('\n');

    return `${atividadeBase}\n${musicas}`;
  }

  private montarColunasOperacionais(etapa: RelatorioCultoEtapa, escalasEtapa: any[]): Record<ColunaOperacional, string[]> {
    const colunas: Record<ColunaOperacional, string[]> = {
      pulpito: [],
      iluminacao: [],
      telao: [],
      som: [],
      atmosfera: []
    };

    for (const acao of etapa.acoesMinisterio || []) {
      const coluna = this.identificarColuna(acao.ministerioNome, acao.descricaoAcao);
      const ministerio = this.textoValido(acao.ministerioNome) || `Ministério #${acao.ministerioId}`;
      const descricao = this.textoValido(acao.descricaoAcao) || '-';
      const observacao = this.textoValido(acao.observacao);
      colunas[coluna].push(observacao ? `${ministerio}: ${descricao} (${observacao})` : `${ministerio}: ${descricao}`);
    }

    for (const escala of escalasEtapa || []) {
      const coluna = this.identificarColuna(escala.ministerioNome, escala.funcao);
      const nome = this.textoValido(escala.voluntarioNome) || `Voluntário #${escala.voluntarioId}`;
      const funcao = this.textoValido(escala.funcao);
      colunas[coluna].push(funcao ? `${nome} (${funcao})` : nome);
    }

    return colunas;
  }

  private identificarColuna(ministerio: string | null | undefined, contexto: string | null | undefined): ColunaOperacional {
    const texto = `${this.textoValido(ministerio) || ''} ${this.textoValido(contexto) || ''}`
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');

    if (texto.includes('som') || texto.includes('audio')) {
      return 'som';
    }
    if (texto.includes('telao') || texto.includes('midia') || texto.includes('projec') || texto.includes('video')) {
      return 'telao';
    }
    if (texto.includes('ilumin') || texto.includes('luz')) {
      return 'iluminacao';
    }
    if (texto.includes('recepc') || texto.includes('dados') || texto.includes('atmosfera') || texto.includes('apoio') || texto.includes('boas vindas')) {
      return 'atmosfera';
    }
    return 'pulpito';
  }

  private montarLinhasRepertorio(itens: RelatorioCultoRepertorioItem[], etapas: RelatorioCultoEtapa[]): LinhaRepertorio[] {
    const mapaEtapas = new Map<number, string>();
    for (const etapa of etapas || []) {
      mapaEtapas.set(Number(etapa.id), `#${etapa.sequencia} ${etapa.atividade || 'Etapa'}`);
    }

    return (itens || [])
      .slice()
      .sort((a, b) => Number(a.ordem || 0) - Number(b.ordem || 0))
      .map((item) => ({
        ordem: Number(item.ordem || 0),
        etapa: item.etapaCultoId ? (mapaEtapas.get(Number(item.etapaCultoId)) || 'Sem etapa vinculada') : 'Sem etapa vinculada',
        musica: item.musicaTitulo || `Música #${item.musicaId}`,
        tom: this.textoValido(item.musicaTom ?? item.tom) || '-',
        responsavel: this.textoValido(item.responsavel) || '-',
        observacoes: this.textoValido(item.observacoes) || '-'
      }));
  }

  private montarDocumentoPdf(logoDataUrl?: string | null): any {
    const corpoCronograma = [
      ['HORÁRIO', 'DURAÇÃO', 'ATIVIDADE', 'PÚLPITO / MICROFONE', 'ILUMINAÇÃO SALÃO', 'TELÃO', 'SOM', 'ATMOSFERA', 'OBSERVAÇÃO'],
      ...this.linhasCronograma.map((linha) => [
        linha.horario,
        linha.duracao,
        linha.atividade,
        linha.pulpitoMicrofone,
        linha.iluminacaoSalao,
        linha.telao,
        linha.som,
        linha.atmosfera,
        linha.observacao
      ])
    ];

    const secoesMenores = this.montarSecoesMenoresPreview();
    const blocosMenores = secoesMenores.map((secao) => {
      const paleta = this.paletaSecao(secao.tipo, secao.variante);
      const totalColunas = Math.max(1, secao.cabecalho.length);
      const linhaTitulo = [
        {
          text: secao.titulo,
          bold: true,
          color: '#FFFFFF',
          fillColor: paleta.tituloHex,
          alignment: 'left',
          margin: [4, 2, 4, 2],
          colSpan: totalColunas
        },
        ...Array.from({ length: Math.max(0, totalColunas - 1) }, () => ({}))
      ];
      const linhaCabecalho = secao.cabecalho.map((titulo) => ({
        text: titulo,
        bold: true,
        color: '#FFFFFF',
        fillColor: paleta.cabecalhoHex,
        alignment: 'center'
      }));

      const corpo = [
        linhaTitulo,
        linhaCabecalho,
        ...secao.linhas.map((linha) => linha.map((valor) => ({ text: valor })))
      ];

      const widths = this.largurasPdfSecaoMenor(secao);

      return {
        table: {
          headerRows: 2,
          widths,
          body: corpo
        },
        layout: 'lightHorizontalLines',
        margin: [0, 0, 0, 8]
      };
    });

    const cabecalhoTopo = logoDataUrl
      ? {
          columns: [
            { image: logoDataUrl, width: 52, margin: [0, 0, 0, 0] },
            {
              width: '*',
              stack: [
                { text: 'CRONOGRAMA DE CULTO', style: 'tituloTopo' },
                { text: `DATA: ${this.dataTitulo}`, style: 'dataTopo' }
              ]
            }
          ],
          columnGap: 8,
          margin: [0, 0, 0, 6]
        }
      : {
          stack: [
            { text: 'CRONOGRAMA DE CULTO', style: 'tituloTopo' },
            { text: `DATA: ${this.dataTitulo}`, style: 'dataTopo' }
          ],
          margin: [0, 0, 0, 6]
        };

    const content: any[] = [
      cabecalhoTopo,
      {
        table: {
          headerRows: 1,
          widths: [44, 40, 112, 86, 78, 64, 72, 90, '*'],
          body: corpoCronograma
        },
        layout: 'lightHorizontalLines'
      }
    ];

    for (let index = 0; index < blocosMenores.length; index += 2) {
      const esquerda = blocosMenores[index];
      const direita = blocosMenores[index + 1];
      content.push({
        columns: direita ? [esquerda, direita] : [esquerda],
        columnGap: 10
      });
    }

    return {
      pageSize: 'A4',
      pageOrientation: 'landscape',
      pageMargins: [16, 16, 16, 16],
      content,
      styles: {
        tituloTopo: {
          fontSize: 13,
          bold: true,
          margin: [0, 0, 0, 2],
          color: '#111827'
        },
        dataTopo: {
          fontSize: 10,
          bold: true,
          margin: [0, 0, 0, 8]
        },
        secaoAzul: {
          fontSize: 10,
          bold: true,
          margin: [0, 8, 0, 4],
          color: '#1F3F66'
        },
        secaoVerde: {
          fontSize: 10,
          bold: true,
          margin: [0, 8, 0, 4],
          color: '#166534'
        }
      },
      defaultStyle: {
        fontSize: 7
      }
    };
  }

  private montarDocumentoPdfCards(logoDataUrl?: string | null): any {
    const content: any[] = [
      this.montarCabecalhoPdfCards(logoDataUrl),
      {
        columns: [
          this.montarResumoPdfCards('Data', this.dataTitulo, '#1D4ED8'),
          this.montarResumoPdfCards('Horario', this.intervaloCronograma, '#7C3AED'),
          this.montarResumoPdfCards('Etapas', String(this.linhasCronograma.length), '#059669')
        ],
        columnGap: 8,
        margin: [0, 0, 0, 12]
      }
    ];

    if (this.linhasCronograma.length) {
      content.push({ text: 'Timeline do culto', style: 'secaoCards' });
      for (const linha of this.linhasCronograma) {
        const cor = this.corPdfLinhaCard(linha);
        content.push(this.montarCardPdf([
          {
            columns: [
              { text: linha.horario, width: 52, style: 'horaCard', color: cor },
              {
                width: '*',
                stack: [
                  {
                    columns: [
                      { text: linha.atividade, style: 'tituloCard' },
                      { text: linha.duracao, width: 58, alignment: 'right', style: 'tagCard', color: cor }
                    ]
                  },
                  this.montarDetalhesOperacionaisPdfCards(linha),
                  ...(linha.observacao && linha.observacao !== '-' ? [{ text: linha.observacao, style: 'obsCard' }] : [])
                ]
              }
            ],
            columnGap: 8
          }
        ], cor));
      }
    }

    if (this.cronogramasAdicionais.length) {
      content.push({ text: 'Cronogramas adicionais', style: 'secaoCards' });
      this.adicionarCardsEmGridPdf(content, this.cronogramasAdicionais.map((secao) => this.montarCardPdf([
        { text: secao.titulo, style: 'tituloCard' },
        ...secao.linhas.map((linha) => ({
          stack: [
            { text: `${linha.horario} - ${linha.atividade}`, bold: true, margin: [0, 4, 0, 1] },
            { text: linha.voluntarios.join(' | ') || '-', color: '#475569' }
          ]
        }))
      ], '#2563EB')));
    }

    if (this.secoesVoluntariosMinisterio.length || this.linhasLideresEquipe.length) {
      content.push({ text: 'Ministerios e lideranca', style: 'secaoCards' });
      const cardsMinisterios = this.secoesVoluntariosMinisterio.map((secao, index) => this.montarCardPdf([
        { text: secao.titulo, style: 'tituloCard' },
        ...secao.voluntarios.map((voluntario) => ({
          columns: [
            { text: voluntario.nome, width: '*', margin: [0, 4, 0, 0] },
            { text: voluntario.funcao, width: 120, alignment: 'right', bold: true, color: '#334155', margin: [0, 4, 0, 0] }
          ]
        }))
      ], this.corPdfMinisterio(index)));

      if (this.linhasLideresEquipe.length) {
        cardsMinisterios.push(this.montarCardPdf([
          { text: 'Lideres por equipe', style: 'tituloCard' },
          ...this.linhasLideresEquipe.map((linha) => ({
            columns: [
              { text: linha.equipe, width: '*', margin: [0, 4, 0, 0] },
              { text: linha.lideres, width: 130, alignment: 'right', bold: true, color: '#334155', margin: [0, 4, 0, 0] }
            ]
          }))
        ], '#0F766E'));
      }

      this.adicionarCardsEmGridPdf(content, cardsMinisterios);
    }

    if (this.linhasRepertorio.length) {
      content.push({ text: 'Repertorio', style: 'secaoCards' });
      this.adicionarCardsEmGridPdf(content, this.linhasRepertorio.map((linha) => this.montarCardPdf([
        {
          columns: [
            { text: String(linha.ordem), width: 28, style: 'numeroMusica' },
            {
              width: '*',
              stack: [
                { text: linha.musica, style: 'tituloCard' },
                { text: `${linha.etapa} - Tom: ${linha.tom} - ${linha.responsavel}`, color: '#475569', margin: [0, 2, 0, 0] },
                ...(linha.observacoes && linha.observacoes !== '-' ? [{ text: linha.observacoes, style: 'obsCard' }] : [])
              ]
            }
          ],
          columnGap: 8
        }
      ], '#16A34A')));
    }

    return {
      pageSize: 'A4',
      pageOrientation: 'portrait',
      pageMargins: [24, 22, 24, 22],
      content,
      styles: {
        tituloCards: { fontSize: 20, bold: true, color: '#0F172A', margin: [0, 0, 0, 2] },
        subtituloCards: { fontSize: 10, color: '#64748B' },
        secaoCards: { fontSize: 13, bold: true, color: '#0F172A', margin: [0, 10, 0, 6] },
        tituloCard: { fontSize: 10, bold: true, color: '#0F172A' },
        horaCard: { fontSize: 11, bold: true },
        tagCard: { fontSize: 8, bold: true },
        obsCard: { fontSize: 8, color: '#64748B', italics: true, margin: [0, 5, 0, 0] },
        numeroMusica: { fontSize: 14, bold: true, color: '#16A34A', alignment: 'center' }
      },
      defaultStyle: {
        fontSize: 8,
        color: '#1E293B'
      }
    };
  }

  private montarCabecalhoPdfCards(logoDataUrl?: string | null): any {
    const texto = {
      width: '*',
      stack: [
        { text: this.cultoNomeCards, style: 'tituloCards' },
        { text: `Cronograma completo - ${this.dataTitulo}`, style: 'subtituloCards' }
      ]
    };

    return logoDataUrl
      ? { columns: [{ image: logoDataUrl, width: 48 }, texto], columnGap: 10, margin: [0, 0, 0, 12] }
      : { stack: [texto], margin: [0, 0, 0, 12] };
  }

  private montarResumoPdfCards(rotulo: string, valor: string, cor: string): any {
    return this.montarCardPdf([
      { text: rotulo.toUpperCase(), fontSize: 7, bold: true, color: '#64748B' },
      { text: valor, fontSize: 12, bold: true, color: cor, margin: [0, 3, 0, 0] }
    ], cor, [7, 6, 7, 6]);
  }

  private montarDetalhesOperacionaisPdfCards(linha: LinhaCronogramaModelo): any {
    const itens = [
      ['Pulpito/Microfone', linha.pulpitoMicrofone],
      ['Iluminacao', linha.iluminacaoSalao],
      ['Telao', linha.telao],
      ['Som', linha.som]
    ];

    return {
      columns: itens.map(([rotulo, valor]) => ({
        width: '*',
        stack: [
          { text: rotulo, fontSize: 6, bold: true, color: '#64748B', margin: [0, 5, 0, 1] },
          { text: valor || '-', fontSize: 7, color: '#334155' }
        ]
      })),
      columnGap: 6
    };
  }

  private montarCardPdf(stack: any[], cor: string, marginConteudo: [number, number, number, number] = [8, 7, 8, 7]): any {
    return {
      table: {
        widths: ['*'],
        body: [[{ stack, margin: marginConteudo }]]
      },
      layout: {
        hLineWidth: () => 0.8,
        vLineWidth: () => 0.8,
        hLineColor: () => cor,
        vLineColor: () => cor,
        fillColor: () => '#FFFFFF'
      },
      margin: [0, 0, 0, 8]
    };
  }

  private adicionarCardsEmGridPdf(content: any[], cards: any[]): void {
    for (let index = 0; index < cards.length; index += 2) {
      const esquerda = cards[index];
      const direita = cards[index + 1];
      content.push({
        columns: direita ? [esquerda, direita] : [esquerda],
        columnGap: 10
      });
    }
  }

  private corPdfLinhaCard(linha: LinhaCronogramaModelo): string {
    const classe = this.categoriaLinhaClasse(linha);
    const cores: Record<string, string> = {
      'cat-fim': '#64748B',
      'cat-pal': '#7C3AED',
      'cat-lou': '#2563EB',
      'cat-esp': '#EA580C',
      'cat-ent': '#0891B2',
      'cat-org': '#16A34A'
    };
    return cores[classe] || '#2563EB';
  }

  private corPdfMinisterio(index: number): string {
    const cores = ['#EF6C00', '#00897B', '#6D4C41'];
    return cores[Math.abs(Number(index) || 0) % cores.length];
  }

  private bordaPadrao(): any {
    return {
      top: { style: 'thin', color: { argb: 'FF7A7A7A' } },
      left: { style: 'thin', color: { argb: 'FF7A7A7A' } },
      bottom: { style: 'thin', color: { argb: 'FF7A7A7A' } },
      right: { style: 'thin', color: { argb: 'FF7A7A7A' } }
    };
  }

  private baixarArquivo(buffer: ArrayBuffer, nomeArquivo: string, mimeType: string): void {
    const blob = new Blob([buffer], { type: mimeType });
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = nomeArquivo;
    anchor.click();
    setTimeout(() => window.URL.revokeObjectURL(url), 500);
  }

  private montarFaixaHorario(etapa: RelatorioCultoEtapa): string {
    const inicio = this.horaTexto(etapa.horarioInicio);
    return inicio;
  }

  private juntarObservacoes(...itens: Array<string | null | undefined>): string {
    const partes = itens
      .map((item) => this.textoValido(item))
      .filter((item): item is string => !!item);
    return partes.length ? partes.join(' | ') : '-';
  }

  private formatarData(valor: string | Date | null | undefined): string {
    if (!valor) {
      return '-';
    }
    const data = new Date(valor);
    if (Number.isNaN(data.getTime())) {
      return String(valor);
    }
    return data.toLocaleDateString('pt-BR');
  }

  private compararCultosDesc(a: Culto, b: Culto): number {
    const dataA = `${a.dataCulto || ''}T${this.horaTexto(a.horarioInicio)}:00`;
    const dataB = `${b.dataCulto || ''}T${this.horaTexto(b.horarioInicio)}:00`;
    const valorA = new Date(dataA).getTime();
    const valorB = new Date(dataB).getTime();
    return valorB - valorA;
  }

  private compararHorarioTexto(a: string, b: string): number {
    const pa = (a || '').match(/^(\d{2}):(\d{2})/);
    const pb = (b || '').match(/^(\d{2}):(\d{2})/);
    if (!pa && !pb) {
      return 0;
    }
    if (!pa) {
      return 1;
    }
    if (!pb) {
      return -1;
    }

    const ma = Number(pa[1]) * 60 + Number(pa[2]);
    const mb = Number(pb[1]) * 60 + Number(pb[2]);
    return ma - mb;
  }

  private normalizarBloco(valor: string | null | undefined): string {
    const texto = String(valor || '').trim();
    return texto ? texto.toUpperCase() : 'PRINCIPAL';
  }

  cabecalhosVoluntarios(secao: SecaoCronogramaAdicional): string[] {
    return Array.from({ length: secao.quantidadeVoluntarios }, (_, index) => `Voluntário ${index + 1}`);
  }

  classeCorMinisterio(index: number): string {
    const variantes = ['variante-a', 'variante-b', 'variante-c'];
    return variantes[Math.abs(Number(index) || 0) % variantes.length];
  }

  private montarSecoesMenoresPreview(): SecaoMenorPreview[] {
    const secoesAdicionais: SecaoMenorPreview[] = this.cronogramasAdicionais.map((secao) => ({
      titulo: secao.titulo,
      tipo: 'adicional',
      variante: 0,
      cabecalho: ['TEMPO', 'ATIVIDADE', ...Array.from({ length: secao.quantidadeVoluntarios }, (_, index) => `Voluntário ${index + 1}`)],
      linhas: secao.linhas.map((linha) => [linha.horario, linha.atividade, ...linha.voluntarios])
    }));

    const secoesMinisterio: SecaoMenorPreview[] = this.secoesVoluntariosMinisterio.map((secao, index) => ({
      titulo: secao.titulo,
      tipo: 'ministerio',
      variante: index % 3,
      cabecalho: ['VOLUNTÁRIO', 'FUNÇÃO'],
      linhas: secao.voluntarios.map((item) => [item.nome, item.funcao])
    }));

    const secoesLideres: SecaoMenorPreview[] = this.linhasLideresEquipe.length
      ? [{
          titulo: 'LÍDERES POR EQUIPE',
          tipo: 'lideres',
          variante: 0,
          cabecalho: ['EQUIPE', 'LÍDER(ES)'],
          linhas: this.linhasLideresEquipe.map((linha) => [linha.equipe, linha.lideres])
        }]
      : [];

    const secoesRepertorio: SecaoMenorPreview[] = this.linhasRepertorio.length
      ? [{
          titulo: 'REPERTÓRIO',
          tipo: 'repertorio',
          variante: 0,
          cabecalho: ['ORDEM', 'ETAPA', 'MÚSICA', 'TOM', 'RESPONSÁVEL', 'OBSERVAÇÕES'],
          linhas: this.linhasRepertorio.map((linha) => [
            String(linha.ordem),
            linha.etapa,
            linha.musica,
            linha.tom,
            linha.responsavel,
            linha.observacoes
          ])
        }]
      : [];

    return [...secoesAdicionais, ...secoesMinisterio, ...secoesLideres, ...secoesRepertorio];
  }

  private escreverSecaoMenorXlsx(sheet: any, linhaInicial: number, colunaInicial: number, larguraBloco: number, secao: SecaoMenorPreview): number {
    const paleta = this.paletaSecao(secao.tipo, secao.variante);
    const totalColunas = Math.max(1, secao.cabecalho.length);
    const colunaFimTitulo = colunaInicial + totalColunas - 1;
    const linhaTitulo = linhaInicial;
    const linhaCabecalho = linhaInicial + 1;

    sheet.mergeCells(`${this.colunaExcel(colunaInicial)}${linhaTitulo}:${this.colunaExcel(colunaFimTitulo)}${linhaTitulo}`);
    const cellTitulo = sheet.getCell(`${this.colunaExcel(colunaInicial)}${linhaTitulo}`);
    cellTitulo.value = secao.titulo;
    cellTitulo.font = { bold: true, color: { argb: 'FFFFFFFF' } };
    cellTitulo.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: paleta.tituloArgb } };
    cellTitulo.alignment = { horizontal: 'left', vertical: 'middle' };
    cellTitulo.border = this.bordaPadrao();

    secao.cabecalho.forEach((valor, index) => {
      const coluna = colunaInicial + index;
      const cell = sheet.getCell(`${this.colunaExcel(coluna)}${linhaCabecalho}`);
      cell.value = valor;
      cell.font = { bold: true, color: { argb: 'FFFFFFFF' } };
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: paleta.cabecalhoArgb } };
      cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
      cell.border = this.bordaPadrao();
    });

    let linhaAtual = linhaCabecalho + 1;
    for (const linha of secao.linhas) {
      linha.forEach((valor, index) => {
        const coluna = colunaInicial + index;
        const cell = sheet.getCell(`${this.colunaExcel(coluna)}${linhaAtual}`);
        cell.value = valor;
        cell.alignment = { horizontal: 'left', vertical: 'top', wrapText: true };
        cell.border = this.bordaPadrao();
      });
      linhaAtual += 1;
    }

    for (let coluna = colunaInicial; coluna < colunaInicial + Math.min(larguraBloco, totalColunas); coluna += 1) {
      const ultimaCell = sheet.getCell(`${this.colunaExcel(coluna)}${Math.max(linhaCabecalho + 1, linhaAtual - 1)}`);
      ultimaCell.border = this.bordaPadrao();
    }

    return Math.max(linhaAtual - 1, linhaCabecalho + 1);
  }

  private largurasPdfSecaoMenor(secao: SecaoMenorPreview): Array<number | string> {
    if (secao.tipo === 'repertorio') {
      return [36, 88, '*', 38, 70, 82];
    }

    if (secao.tipo === 'lideres') {
      return [120, '*'];
    }

    if (secao.tipo === 'adicional') {
      return [44, '*', ...Array.from({ length: Math.max(0, secao.cabecalho.length - 2) }, () => 70)];
    }

    if (secao.tipo === 'ministerio') {
      return [120, '*'];
    }

    return ['*'];
  }

  private paletaSecao(tipo: SecaoMenorPreview['tipo'], variante: number): { tituloArgb: string; cabecalhoArgb: string; tituloHex: string; cabecalhoHex: string } {
    if (tipo === 'adicional') {
      return { tituloArgb: 'FF2F75E8', cabecalhoArgb: 'FF1E5EC4', tituloHex: '#2F75E8', cabecalhoHex: '#1E5EC4' };
    }

    if (tipo === 'lideres') {
      return { tituloArgb: 'FF1565C0', cabecalhoArgb: 'FF0F4F9E', tituloHex: '#1565C0', cabecalhoHex: '#0F4F9E' };
    }

    if (tipo === 'repertorio') {
      return { tituloArgb: 'FF2E7D32', cabecalhoArgb: 'FF1E5A22', tituloHex: '#2E7D32', cabecalhoHex: '#1E5A22' };
    }

    const variantesMinisterio = [
      { tituloArgb: 'FFEF6C00', cabecalhoArgb: 'FFCC5D00', tituloHex: '#EF6C00', cabecalhoHex: '#CC5D00' },
      { tituloArgb: 'FF00897B', cabecalhoArgb: 'FF007064', tituloHex: '#00897B', cabecalhoHex: '#007064' },
      { tituloArgb: 'FF6D4C41', cabecalhoArgb: 'FF5A3E35', tituloHex: '#6D4C41', cabecalhoHex: '#5A3E35' }
    ];
    return variantesMinisterio[Math.abs(Number(variante) || 0) % variantesMinisterio.length];
  }

  private colunaExcel(numero: number): string {
    let atual = Math.max(1, Math.floor(numero));
    let resultado = '';

    while (atual > 0) {
      const resto = (atual - 1) % 26;
      resultado = String.fromCharCode(65 + resto) + resultado;
      atual = Math.floor((atual - 1) / 26);
    }

    return resultado;
  }

  private normalizarNomeArquivo(nome: string): string {
    return (nome || 'culto')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  private dataCultoParaNomeArquivo(): string {
    const dataBase = this.relatorio?.cultos?.[0]?.culto?.dataCulto || this.relatorio?.data || new Date().toISOString();
    const data = new Date(dataBase);

    if (Number.isNaN(data.getTime())) {
      const agora = new Date();
      return `${`${agora.getDate()}`.padStart(2, '0')}${`${agora.getMonth() + 1}`.padStart(2, '0')}${agora.getFullYear()}`;
    }

    return `${`${data.getDate()}`.padStart(2, '0')}${`${data.getMonth() + 1}`.padStart(2, '0')}${data.getFullYear()}`;
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

  private async carregarLogoPdfDataUrl(): Promise<string | null> {
    if (this.logoPdfDataUrl) {
      return this.logoPdfDataUrl;
    }

    try {
      const response = await fetch('assets/logo-vertical.png');
      if (!response.ok) {
        return null;
      }

      const blob = await response.blob();
      const dataUrl = await this.blobParaDataUrl(blob);
      this.logoPdfDataUrl = dataUrl;
      return dataUrl;
    } catch {
      return null;
    }
  }

  private blobParaDataUrl(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const leitor = new FileReader();
      leitor.onloadend = () => {
        const resultado = leitor.result;
        if (typeof resultado === 'string') {
          resolve(resultado);
          return;
        }
        reject(new Error('Falha ao converter logo para data URL.'));
      };
      leitor.onerror = () => reject(leitor.error || new Error('Falha ao ler logo.'));
      leitor.readAsDataURL(blob);
    });
  }
}
