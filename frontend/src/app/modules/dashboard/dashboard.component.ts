import { Component, OnInit } from '@angular/core';
import { DashboardResumo } from '../../core/models/dashboard.models';
import { DashboardService } from '../../core/services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  carregando = false;
  resumo: DashboardResumo | null = null;

  constructor(private readonly dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.carregar();
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
}
