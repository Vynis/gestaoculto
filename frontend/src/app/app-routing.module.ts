import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './modules/auth/login/login.component';
import { RecuperarSenhaComponent } from './modules/auth/recuperar-senha/recuperar-senha.component';
import { TrocarSenhaObrigatoriaComponent } from './modules/auth/trocar-senha-obrigatoria/trocar-senha-obrigatoria.component';
import { MainLayoutComponent } from './layout/main-layout.component';
import { VoluntarioLayoutComponent } from './layout/voluntario-layout.component';
import { AuthGuard } from './core/guards/auth.guard';
import { GestaoGuard } from './core/guards/gestao.guard';
import { VoluntarioGuard } from './core/guards/voluntario.guard';
import { LouvorGuard } from './core/guards/louvor.guard';
import { DashboardComponent } from './modules/dashboard/dashboard.component';
import { CultosComponent } from './modules/cultos/cultos.component';
import { RecorrenciasCultoComponent } from './modules/recorrencias-culto/recorrencias-culto.component';
import { CronogramaComponent } from './modules/cronograma/cronograma.component';
import { EscalasComponent } from './modules/escalas/escalas.component';
import { ConvidadosComponent } from './modules/convidados/convidados.component';
import { TemplatesCultoComponent } from './modules/templates-culto/templates-culto.component';
import { VoluntariosComponent } from './modules/voluntarios/voluntarios.component';
import { MinisteriosComponent } from './modules/ministerios/ministerios.component';
import { MusicasComponent } from './modules/musicas/musicas.component';
import { RepertorioComponent } from './modules/repertorio/repertorio.component';
import { UsuariosComponent } from './modules/usuarios/usuarios.component';
import { RelatorioCultoComponent } from './modules/relatorio-culto/relatorio-culto.component';
import { RelatorioCultoPublicoPageComponent } from './modules/relatorio-culto/relatorio-culto-publico-page.component';
import { RelatorioEscalaMensalComponent } from './modules/relatorio-escala-mensal/relatorio-escala-mensal.component';
import { DisponibilidadesComponent } from './modules/disponibilidades/disponibilidades.component';
import { LoginVoluntarioComponent } from './modules/voluntario-auth/login-voluntario.component';
import { PrimeiroAcessoVoluntarioComponent } from './modules/voluntario-auth/primeiro-acesso-voluntario.component';
import { RecuperarAcessoVoluntarioComponent } from './modules/voluntario-auth/recuperar-acesso-voluntario.component';
import { PainelVoluntarioComponent } from './modules/voluntario/painel-voluntario.component';
import { CalendarioVoluntarioComponent } from './modules/voluntario/calendario-voluntario.component';
import { EscalaVoluntarioComponent } from './modules/voluntario/escala-voluntario.component';
import { MinisteriosVoluntarioComponent } from './modules/voluntario/ministerios-voluntario.component';
import { ColegasMinisterioVoluntarioComponent } from './modules/voluntario/colegas-ministerio-voluntario.component';
import { MeusDadosVoluntarioComponent } from './modules/voluntario/meus-dados-voluntario.component';
import { RepertorioVoluntarioComponent } from './modules/voluntario/repertorio-voluntario.component';
import { AppInfoPageComponent } from './modules/publico/app-info-page.component';
import { PoliticaPrivacidadePageComponent } from './modules/publico/politica-privacidade-page.component';
import { TermosUsoPageComponent } from './modules/publico/termos-uso-page.component';
import { RepertorioCanDeactivateGuard } from './core/guards/repertorio-can-deactivate.guard';

const routes: Routes = [
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/recuperar-senha', component: RecuperarSenhaComponent },
  { path: 'auth/trocar-senha', component: TrocarSenhaObrigatoriaComponent, canActivate: [AuthGuard] },
  { path: 'voluntario/login', component: LoginVoluntarioComponent },
  { path: 'voluntario/primeiro-acesso', component: PrimeiroAcessoVoluntarioComponent },
  { path: 'voluntario/recuperar-acesso', component: RecuperarAcessoVoluntarioComponent },
  { path: 'relatorio-culto-publico', component: RelatorioCultoPublicoPageComponent },
  { path: 'app-info', component: AppInfoPageComponent },
  { path: 'politica-de-privacidade', component: PoliticaPrivacidadePageComponent },
  { path: 'termos-de-uso', component: TermosUsoPageComponent },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard, GestaoGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'cultos', component: CultosComponent },
      { path: 'recorrencias-culto', component: RecorrenciasCultoComponent },
      { path: 'templates', component: TemplatesCultoComponent },
      { path: 'ministerios', component: MinisteriosComponent },
      { path: 'cronograma', component: CronogramaComponent },
      { path: 'relatorio-culto', component: RelatorioCultoComponent },
      { path: 'relatorio-escala-mensal', component: RelatorioEscalaMensalComponent },
      { path: 'escalas', component: EscalasComponent },
      { path: 'disponibilidades', component: DisponibilidadesComponent },
      { path: 'musicas', component: MusicasComponent },
      { path: 'repertorio', component: RepertorioComponent, canDeactivate: [RepertorioCanDeactivateGuard] },
      { path: 'usuarios', component: UsuariosComponent },
      { path: 'voluntarios', component: VoluntariosComponent },
      { path: 'convidados', component: ConvidadosComponent }
    ]
  },
  {
    path: 'voluntario',
    component: VoluntarioLayoutComponent,
    canActivate: [AuthGuard, VoluntarioGuard],
    children: [
      { path: '', redirectTo: 'painel', pathMatch: 'full' },
      { path: 'painel', component: PainelVoluntarioComponent },
      { path: 'calendario', component: CalendarioVoluntarioComponent },
      { path: 'minha-escala', component: EscalaVoluntarioComponent },
      { path: 'repertorio', component: RepertorioVoluntarioComponent, canActivate: [LouvorGuard] },
      { path: 'repertorio/culto/:cultoId', component: RepertorioVoluntarioComponent, canActivate: [LouvorGuard] },
      { path: 'repertorio/escala/:escalaId', component: RepertorioVoluntarioComponent, canActivate: [LouvorGuard] },
      { path: 'meus-ministerios', component: MinisteriosVoluntarioComponent },
      { path: 'colegas', component: ColegasMinisterioVoluntarioComponent },
      { path: 'meus-dados', component: MeusDadosVoluntarioComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
