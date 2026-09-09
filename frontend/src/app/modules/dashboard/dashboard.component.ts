import { Component, OnInit } from '@angular/core';
import { DashboardEstatisticas, DashboardResumo } from '../../core/models/dashboard.models';
import { DashboardService } from '../../core/services/dashboard.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  carregando = false;
  resumo: DashboardResumo | null = null;
  estatisticas: DashboardEstatisticas | null = null;
  carregandoEstatisticas = false;
  erroEstatisticas = false;
  dataInicio = `${new Date().getFullYear()}-01-01`;
  dataFim = this.formatarData(new Date());
  podeVerEstatisticas = false;

  constructor(
    private readonly dashboardService: DashboardService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.carregar();
    this.podeVerEstatisticas = this.authService.possuiAlgumPerfil(['ADMIN', 'GESTAO_CULTO', 'LIDER_MINISTERIO']);
    if (this.podeVerEstatisticas) {
      this.carregarEstatisticas();
    }
  }

  carregar(): void {
    this.carregando = true;
    this.dashboardService.obterResumo().subscribe({
      next: (resumo) => {
        this.resumo = resumo;
      },
      complete: () => {
        this.carregando = false;
      }
    });
  }

  carregarEstatisticas(): void {
    if (!this.dataInicio || !this.dataFim || this.dataInicio > this.dataFim) {
      this.erroEstatisticas = true;
      return;
    }

    this.carregandoEstatisticas = true;
    this.erroEstatisticas = false;
    this.dashboardService.obterEstatisticas(this.dataInicio, this.dataFim).subscribe({
      next: (estatisticas) => {
        this.estatisticas = estatisticas;
      },
      error: () => {
        this.erroEstatisticas = true;
        this.carregandoEstatisticas = false;
      },
      complete: () => {
        this.carregandoEstatisticas = false;
      }
    });
  }

  maiorQuantidade(itens: { quantidadeCultos: number }[]): number {
    return itens.length ? Math.max(...itens.map((item) => item.quantidadeCultos)) : 1;
  }

  larguraBarra(quantidade: number, itens: { quantidadeCultos: number }[]): number {
    return Math.max(8, Math.round((quantidade / this.maiorQuantidade(itens)) * 100));
  }

  formatarDiaMes(data: string): string {
    const valor = data?.substring(0, 10) || '';
    const [ano, mes, dia] = valor.split('-');
    return ano && mes && dia ? `${dia}/${mes}` : 'Data não informada';
  }

  private formatarData(data: Date): string {
    return `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, '0')}-${String(data.getDate()).padStart(2, '0')}`;
  }
}
