import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { NbToastrService } from '@nebular/theme';
import { CadastroService } from '../../core/services/cadastro.service';
import { RelatorioService } from '../../core/services/relatorio.service';
import { Ministerio, Voluntario } from '../../core/models/cadastro.models';
import { RelatorioEscalaMensalItem, RelatorioEscalaMensalResponse } from '../../core/models/relatorio-culto.models';

interface GrupoEscalaMensal {
  data: string;
  itens: RelatorioEscalaMensalItem[];
}

@Component({
  selector: 'app-relatorio-escala-mensal',
  templateUrl: './relatorio-escala-mensal.component.html',
  styleUrls: ['./relatorio-escala-mensal.component.scss']
})
export class RelatorioEscalaMensalComponent implements OnInit {
  ministerios: Ministerio[] = [];
  voluntarios: Voluntario[] = [];
  relatorio: RelatorioEscalaMensalResponse | null = null;
  carregando = false;
  copiando = false;

  readonly status = [
    { id: 1, nome: 'Pendente' },
    { id: 2, nome: 'Confirmado' },
    { id: 3, nome: 'Ausente' }
  ];

  readonly filtro = this.fb.group({
    mes: [this.mesAtual()],
    ministerioId: [null as number | null],
    voluntarioId: [null as number | null],
    presencaStatusId: [null as number | null]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly relatorioService: RelatorioService,
    private readonly cadastroService: CadastroService,
    private readonly toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.cadastroService.listarMinisterios().subscribe((data) => {
      this.ministerios = (data || []).filter((item) => item.ativo).sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
    });
    this.cadastroService.listarVoluntarios().subscribe((data) => {
      this.voluntarios = (data || []).filter((item) => item.ativo).sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
    });
    this.gerar();
  }

  gerar(): void {
    const mes = String(this.filtro.value.mes || '').trim();
    if (!/^\d{4}-\d{2}$/.test(mes)) {
      this.toastr.warning('Selecione um mês válido para gerar o relatório.', 'Relatório');
      return;
    }

    this.carregando = true;
    this.relatorioService.obterRelatorioEscalaMensal(
      mes,
      this.filtro.value.ministerioId,
      this.filtro.value.voluntarioId,
      this.filtro.value.presencaStatusId
    ).subscribe({
      next: (data) => {
        this.relatorio = data;
      },
      error: (error) => {
        this.relatorio = null;
        this.toastr.danger(error?.error?.mensagem || 'Não foi possível gerar o relatório.', 'Erro');
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  get grupos(): GrupoEscalaMensal[] {
    const grupos = new Map<string, RelatorioEscalaMensalItem[]>();
    for (const item of this.relatorio?.itens || []) {
      const chave = item.dataCulto;
      if (!grupos.has(chave)) {
        grupos.set(chave, []);
      }
      grupos.get(chave)!.push(item);
    }
    return Array.from(grupos.entries()).map(([data, itens]) => ({ data, itens }));
  }

  statusTexto(item: RelatorioEscalaMensalItem): string {
    return item.presencaStatusNome || this.status.find((status) => status.id === item.presencaStatusId)?.nome || 'Pendente';
  }

  statusClasse(item: RelatorioEscalaMensalItem): string {
    switch (item.presencaStatusId) {
      case 2: return 'status-confirmado';
      case 3: return 'status-ausente';
      case 4: return 'status-substituido';
      default: return 'status-pendente';
    }
  }

  mesTexto(): string {
    const mes = this.filtro.value.mes;
    if (!mes) {
      return '';
    }
    const [ano, numeroMes] = mes.split('-').map(Number);
    return new Date(ano, numeroMes - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });
  }

  async copiarWhatsApp(): Promise<void> {
    if (!this.relatorio?.itens.length) {
      this.toastr.warning('Gere um relatório com escalas antes de compartilhar.', 'Relatório');
      return;
    }

    this.copiando = true;
    try {
      await navigator.clipboard.writeText(this.textoWhatsApp());
      this.toastr.success('Escala copiada. Agora é só colar no WhatsApp.', 'Relatório');
    } catch {
      this.toastr.warning('Não foi possível copiar automaticamente.', 'Relatório');
    } finally {
      this.copiando = false;
    }
  }

  async exportarExcel(): Promise<void> {
    if (!this.relatorio?.itens.length) {
      this.toastr.warning('Não há escalas para exportar.', 'Relatório');
      return;
    }

    const modulo = await import('exceljs');
    const ExcelJS = (modulo as any).default || modulo;
    const workbook = new ExcelJS.Workbook();
    const sheet = workbook.addWorksheet('Escala mensal');
    sheet.addRow(['ESCALA MENSAL DE VOLUNTÁRIOS']);
    sheet.addRow([this.mesTexto()]);
    sheet.addRow([]);
    sheet.addRow(['DATA', 'HORÁRIO', 'CULTO', 'MINISTÉRIO', 'FUNÇÃO', 'VOLUNTÁRIO', 'STATUS', 'OBSERVAÇÕES']);
    for (const item of this.relatorio.itens) {
      sheet.addRow([
        this.dataTexto(item.dataCulto),
        this.horaTexto(item.horarioInicio),
        item.cultoNome,
        item.ministerioNome || 'Sem ministério',
        item.funcao,
        item.voluntarioNome || 'Voluntário avulso',
        this.statusTexto(item),
        item.observacoes || ''
      ]);
    }
    sheet.getRow(1).font = { bold: true, size: 14 };
    sheet.getRow(4).font = { bold: true, color: { argb: 'FFFFFFFF' } };
    sheet.getRow(4).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF3366CC' } };
    sheet.columns = [10, 10, 28, 22, 20, 24, 16, 32].map((width) => ({ width }));
    const buffer = await workbook.xlsx.writeBuffer();
    this.baixarArquivo(buffer, `escala-mensal-${this.filtro.value.mes}.xlsx`, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');
  }

  async exportarPdf(): Promise<void> {
    if (!this.relatorio?.itens.length) {
      this.toastr.warning('Não há escalas para exportar.', 'Relatório');
      return;
    }

    const pdfMakeModule = await import('pdfmake/build/pdfmake');
    const pdfFontsModule = await import('pdfmake/build/vfs_fonts');
    const pdfMake = (pdfMakeModule as any).default || pdfMakeModule;
    const fontes = (pdfFontsModule as any).default || pdfFontsModule;
    pdfMake.vfs = (fontes as any).pdfMake?.vfs || fontes;
    const body = [
      ['DATA', 'CULTO', 'MINISTÉRIO', 'FUNÇÃO', 'VOLUNTÁRIO', 'STATUS'],
      ...this.relatorio.itens.map((item) => [
        this.dataTexto(item.dataCulto), item.cultoNome, item.ministerioNome || '-', item.funcao,
        item.voluntarioNome || 'Voluntário avulso', this.statusTexto(item)
      ])
    ];
    pdfMake.createPdf({
      pageSize: 'A4',
      pageOrientation: 'landscape',
      pageMargins: [24, 24, 24, 24],
      content: [
        { text: 'ESCALA MENSAL DE VOLUNTÁRIOS', style: 'titulo' },
        { text: this.mesTexto(), margin: [0, 0, 0, 12] },
        { table: { headerRows: 1, widths: [55, '*', '*', '*', '*', 70], body }, layout: 'lightHorizontalLines' }
      ],
      styles: { titulo: { fontSize: 16, bold: true, margin: [0, 0, 0, 4] } },
      defaultStyle: { fontSize: 8 }
    }).download(`escala-mensal-${this.filtro.value.mes}.pdf`);
  }

  private textoWhatsApp(): string {
    const linhas = [`*ESCALA DE VOLUNTÁRIOS*`, `*${this.mesTexto().toUpperCase()}*`, ''];
    for (const grupo of this.grupos) {
      linhas.push(`*${this.dataTexto(grupo.data)}*`);
      const porMinisterio = new Map<string, RelatorioEscalaMensalItem[]>();
      for (const item of grupo.itens) {
        const ministerio = item.ministerioNome || 'Sem ministério';
        if (!porMinisterio.has(ministerio)) porMinisterio.set(ministerio, []);
        porMinisterio.get(ministerio)!.push(item);
      }
      for (const [ministerio, itens] of porMinisterio) {
        linhas.push(`_${ministerio}_`);
        for (const item of itens) {
          linhas.push(`${item.funcao}: ${item.voluntarioNome || 'Voluntário avulso'}`);
        }
        linhas.push('');
      }
    }
    return linhas.join('\n').trim();
  }

  private dataTexto(valor: string): string {
    return new Date(valor).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
  }

  horaTexto(valor: RelatorioEscalaMensalItem['horarioInicio']): string {
    if (valor && typeof valor === 'object') {
      const horas = Number(valor.hours ?? valor.Hours);
      const minutos = Number(valor.minutes ?? valor.Minutes);
      if (!Number.isNaN(horas) && !Number.isNaN(minutos)) {
        return `${String(horas).padStart(2, '0')}:${String(minutos).padStart(2, '0')}`;
      }

      const totalSeconds = Number(valor.totalSeconds ?? valor.TotalSeconds);
      if (!Number.isNaN(totalSeconds)) {
        const horasCalculadas = Math.floor(totalSeconds / 3600) % 24;
        const minutosCalculados = Math.floor((totalSeconds % 3600) / 60);
        return `${String(horasCalculadas).padStart(2, '0')}:${String(minutosCalculados).padStart(2, '0')}`;
      }
    }

    const match = String(valor || '').match(/(\d{1,2}):(\d{2})/);
    return match ? `${match[1].padStart(2, '0')}:${match[2]}` : '--:--';
  }

  private mesAtual(): string {
    const agora = new Date();
    return `${agora.getFullYear()}-${String(agora.getMonth() + 1).padStart(2, '0')}`;
  }

  private baixarArquivo(buffer: ArrayBuffer, nome: string, tipo: string): void {
    const url = window.URL.createObjectURL(new Blob([buffer], { type: tipo }));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = nome;
    anchor.click();
    setTimeout(() => window.URL.revokeObjectURL(url), 500);
  }
}
