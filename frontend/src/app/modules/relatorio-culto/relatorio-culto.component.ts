import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { Router } from '@angular/router';
import { Culto } from '../../core/models/culto.models';
import { MinisterioCompleto } from '../../core/models/ministerio.models';
import { RelatorioCultoDiaResponse, RelatorioCultoEtapa, RelatorioCultoRepertorioItem } from '../../core/models/relatorio-culto.models';
import { MinisterioService } from '../../core/services/ministerio.service';
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
  voluntarios: string[];
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
  ministeriosAtivos: MinisterioCompleto[] = [];

  linhasCronograma: LinhaCronogramaModelo[] = [];
  cronogramasAdicionais: SecaoCronogramaAdicional[] = [];
  secoesVoluntariosMinisterio: SecaoVoluntariosMinisterio[] = [];
  linhasLideresEquipe: LinhaLideresEquipe[] = [];
  linhasRepertorio: LinhaRepertorio[] = [];
  private logoPdfDataUrl: string | null = null;

  readonly filtro = this.fb.group({
    cultoId: [null as number | null, Validators.required]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly relatorioService: RelatorioService,
    private readonly ministerioService: MinisterioService,
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
    const mapaVoluntariosPorMinisterio = new Map<number, { nome: string; voluntarios: Set<string> }>();

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

        if (!mapaVoluntariosPorMinisterio.has(ministerioId)) {
          mapaVoluntariosPorMinisterio.set(ministerioId, {
            nome: ministerioNome,
            voluntarios: new Set<string>()
          });
        }

        mapaVoluntariosPorMinisterio.get(ministerioId)!.voluntarios.add(voluntarioNome);
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
        voluntarios: Array.from(item.voluntarios).sort((a, b) => a.localeCompare(b, 'pt-BR'))
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
      cabecalho: ['VOLUNTÁRIO'],
      linhas: secao.voluntarios.map((nome) => [nome])
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
