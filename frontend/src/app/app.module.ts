import { APP_INITIALIZER, ErrorHandler, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  NbActionsModule,
  NbAutocompleteModule,
  NbBadgeModule,
  NbButtonModule,
  NbCardModule,
  NbCheckboxModule,
  NbDatepickerModule,
  NbIconModule,
  NbInputModule,
  NbLayoutModule,
  NbListModule,
  NbMenuModule,
  NbSelectModule,
  NbSidebarModule,
  NbTabsetModule,
  NbThemeModule,
  NbToastrModule,
  NbUserModule
} from '@nebular/theme';
import { NbEvaIconsModule } from '@nebular/eva-icons';
import { AgGridModule } from 'ag-grid-angular';
import { DragDropModule } from '@angular/cdk/drag-drop';
import * as Sentry from '@sentry/angular';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { LoginComponent } from './modules/auth/login/login.component';
import { RecuperarSenhaComponent } from './modules/auth/recuperar-senha/recuperar-senha.component';
import { TrocarSenhaObrigatoriaComponent } from './modules/auth/trocar-senha-obrigatoria/trocar-senha-obrigatoria.component';
import { MainLayoutComponent } from './layout/main-layout.component';
import { VoluntarioLayoutComponent } from './layout/voluntario-layout.component';
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
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { LoadingInterceptor } from './core/interceptors/loading.interceptor';
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
import { DisponibilidadesComponent } from './modules/disponibilidades/disponibilidades.component';
import { EscalaModalComponent } from './modules/escalas/escala-modal.component';
import { AppInfoPageComponent } from './modules/publico/app-info-page.component';
import { PoliticaPrivacidadePageComponent } from './modules/publico/politica-privacidade-page.component';
import { TermosUsoPageComponent } from './modules/publico/termos-uso-page.component';
import { ExternalLinkViewerComponent } from './shared/external-link-viewer.component';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    RecuperarSenhaComponent,
    TrocarSenhaObrigatoriaComponent,
    MainLayoutComponent,
    VoluntarioLayoutComponent,
    DashboardComponent,
    CultosComponent,
    RecorrenciasCultoComponent,
    CronogramaComponent,
    EscalasComponent,
    ConvidadosComponent,
    TemplatesCultoComponent,
    VoluntariosComponent,
    MinisteriosComponent,
    MusicasComponent,
    RepertorioComponent,
    UsuariosComponent,
    RelatorioCultoComponent,
    RelatorioCultoPublicoPageComponent,
    RelatorioEscalaMensalComponent,
    LoginVoluntarioComponent,
    PrimeiroAcessoVoluntarioComponent,
    RecuperarAcessoVoluntarioComponent,
    PainelVoluntarioComponent,
    CalendarioVoluntarioComponent,
    EscalaVoluntarioComponent,
    MinisteriosVoluntarioComponent,
    ColegasMinisterioVoluntarioComponent,
    MeusDadosVoluntarioComponent,
    RepertorioVoluntarioComponent,
    DisponibilidadesComponent,
    EscalaModalComponent,
    AppInfoPageComponent,
    PoliticaPrivacidadePageComponent,
    TermosUsoPageComponent,
    ExternalLinkViewerComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    AppRoutingModule,
    NbThemeModule.forRoot({ name: 'corporate' }),
    NbSidebarModule.forRoot(),
    NbMenuModule.forRoot(),
    NbToastrModule.forRoot(),
    NbDatepickerModule.forRoot(),
    NbLayoutModule,
    NbAutocompleteModule,
    NbCardModule,
    NbButtonModule,
    NbInputModule,
    NbIconModule,
    NbEvaIconsModule,
    NbActionsModule,
    NbUserModule,
    NbBadgeModule,
    NbSelectModule,
    NbListModule,
    NbCheckboxModule,
    NbTabsetModule,
    AgGridModule,
    DragDropModule
  ],
  providers: [
    {
      provide: ErrorHandler,
      useValue: Sentry.createErrorHandler({
        showDialog: false
      })
    },
    {
      provide: Sentry.TraceService,
      deps: [Router]
    },
    {
      provide: APP_INITIALIZER,
      useFactory: () => () => undefined,
      deps: [Sentry.TraceService],
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoadingInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
