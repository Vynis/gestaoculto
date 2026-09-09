import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

interface WikiArticle {
  category: string;
  title: string;
  summary: string;
  sections: { title: string; paragraphs?: string[]; steps?: string[] }[];
}

@Component({
  selector: 'app-wiki',
  templateUrl: './wiki.component.html',
  styleUrls: ['./wiki.component.scss']
})
export class WikiComponent implements OnInit {
  readonly artigosGestao: WikiArticle[] = [
    {
      category: 'Primeiros passos',
      title: 'Como usar o Gestão Culto',
      summary: 'Entenda o fluxo recomendado para preparar e operar um culto.',
      sections: [
        {
          title: 'Fluxo sugerido',
          steps: [
            'Cadastre ou revise os ministérios e voluntários que participarão da operação.',
            'Crie o culto ou use uma recorrência para gerar eventos futuros.',
            'Monte o cronograma com os momentos do culto e seus responsáveis.',
            'Crie a escala, confira as disponibilidades e acompanhe as confirmações.',
            'Use os relatórios para revisar o culto e compartilhar as informações necessárias.'
          ]
        },
        {
          title: 'Dica',
          paragraphs: ['Templates ajudam a manter um padrão para cultos que seguem uma estrutura parecida.']
        }
      ]
    },
    {
      category: 'Planejamento',
      title: 'Cultos, recorrências e templates',
      summary: 'Aprenda a organizar a agenda de cultos sem repetir trabalho.',
      sections: [
        {
          title: 'Quando usar cada recurso',
          steps: [
            'Use Cultos para criar ou editar uma celebração específica.',
            'Use Recorrências para gerar cultos que se repetem em uma frequência definida.',
            'Use Templates para guardar uma estrutura reutilizável de culto.',
            'Use Cronograma para ordenar os momentos e registrar seus detalhes.'
          ]
        },
        {
          title: 'Atenção',
          paragraphs: ['Depois de gerar uma recorrência, revise as datas e os detalhes de cada culto antes de criar as escalas.']
        }
      ]
    },
    {
      category: 'Escalas e equipes',
      title: 'Criar e acompanhar uma escala',
      summary: 'Organize os voluntários de cada culto com mais segurança.',
      sections: [
        {
          title: 'Passo a passo',
          steps: [
            'Abra Escalas e selecione o culto desejado.',
            'Inclua os ministérios e as funções necessárias para a celebração.',
            'Escolha os voluntários considerando suas disponibilidades.',
            'Revise conflitos de horário e finalize a escala.',
            'Acompanhe a escala publicada pelo relatório ou pelo Portal do Voluntário.'
          ]
        },
        {
          title: 'Problema comum',
          paragraphs: ['Se um voluntário não aparecer, confira se ele está vinculado ao ministério correto e se possui acesso ao portal.']
        }
      ]
    },
    {
      category: 'Músicas e repertório',
      title: 'Organizar músicas e repertórios',
      summary: 'Mantenha o repertório do culto acessível para a equipe de louvor.',
      sections: [
        {
          title: 'Como organizar',
          steps: [
            'Cadastre a música em Músicas, incluindo título, tom e demais informações úteis.',
            'Abra Repertório e associe as músicas ao culto ou à escala.',
            'Ordene as músicas conforme a execução prevista.',
            'Confira o repertório no Portal do Voluntário antes do culto.'
          ]
        }
      ]
    },
    {
      category: 'Relatórios',
      title: 'Consultar e compartilhar relatórios',
      summary: 'Use os relatórios para revisar o culto e comunicar a equipe.',
      sections: [
        {
          title: 'Opções disponíveis',
          steps: [
            'Use Relatório culto para visualizar o cronograma, participantes e repertório de um culto.',
            'Use Escala mensal para acompanhar a distribuição das escalas no período.',
            'Use o compartilhamento público somente quando precisar enviar uma visão externa do relatório.'
          ]
        },
        {
          title: 'Atenção',
          paragraphs: ['Revise os dados antes de compartilhar um relatório, pois o link pode ser acessado por quem o receber.']
        }
      ]
    },
    {
      category: 'Administração',
      title: 'Usuários e permissões',
      summary: 'Saiba como manter os acessos alinhados às responsabilidades da equipe.',
      sections: [
        {
          title: 'Boas práticas',
          steps: [
            'Crie um usuário individual para cada pessoa, sem compartilhar senhas.',
            'Atribua somente os perfis necessários para a função exercida.',
            'Revise os acessos quando alguém mudar de responsabilidade.',
            'Use a recuperação de senha quando o usuário perder o acesso.'
          ]
        }
      ]
    }
  ];

  readonly artigosVoluntario: WikiArticle[] = [
    {
      category: 'Acesso',
      title: 'Primeiro acesso ao portal',
      summary: 'Veja como ativar sua conta e entrar no Portal do Voluntário.',
      sections: [
        {
          title: 'Passo a passo',
          steps: [
            'Acesse a tela de login do Portal do Voluntário.',
            'Escolha Primeiro acesso e informe os dados solicitados.',
            'Defina sua senha e entre novamente usando seu e-mail ou usuário.',
            'Se você já possui uma conta, use Recuperar acesso para criar uma nova senha.'
          ]
        },
        {
          title: 'Problema comum',
          paragraphs: ['Se o cadastro não for localizado, procure a liderança responsável pelo seu ministério para confirmar seus dados.']
        }
      ]
    },
    {
      category: 'Minha escala',
      title: 'Consultar suas escalas',
      summary: 'Encontre seus próximos compromissos e os detalhes de cada escala.',
      sections: [
        {
          title: 'Como consultar',
          steps: [
            'Abra Minha escala para ver suas participações organizadas por data.',
            'Selecione uma escala para conferir culto, horário, função e observações.',
            'Use Calendário para ter uma visão geral dos seus compromissos.',
            'Entre em contato com a liderança se houver alguma informação incorreta.'
          ]
        }
      ]
    },
    {
      category: 'Disponibilidade',
      title: 'Informar sua disponibilidade',
      summary: 'Mantenha seus horários atualizados para ajudar na montagem das escalas.',
      sections: [
        {
          title: 'Passo a passo',
          steps: [
            'Acesse a área de disponibilidade pelo Portal do Voluntário.',
            'Selecione as datas e horários em que você pode servir.',
            'Salve as alterações e revise os períodos informados.',
            'Atualize sua disponibilidade sempre que sua agenda mudar.'
          ]
        },
        {
          title: 'Dica',
          paragraphs: ['Informe indisponibilidades com antecedência para facilitar a organização da equipe.']
        }
      ]
    },
    {
      category: 'Ministérios e equipe',
      title: 'Consultar ministérios e colegas',
      summary: 'Veja os ministérios dos quais você participa e as pessoas da sua equipe.',
      sections: [
        {
          title: 'Onde encontrar',
          steps: [
            'Abra Meus ministérios para consultar suas equipes e funções.',
            'Abra Equipe do ministério para visualizar os colegas vinculados ao ministério.',
            'Use Meus dados para conferir e atualizar suas informações pessoais.'
          ]
        }
      ]
    },
    {
      category: 'Louvor',
      title: 'Consultar o repertório',
      summary: 'Acesse as músicas e orientações do repertório quando essa opção estiver disponível para você.',
      sections: [
        {
          title: 'Como usar',
          steps: [
            'Abra Repertório no menu do portal.',
            'Escolha o culto ou a escala que deseja consultar.',
            'Confira a ordem das músicas, os tons e as observações disponíveis.',
            'Avise a liderança caso alguma informação precise ser corrigida.'
          ]
        }
      ]
    },
    {
      category: 'Calendário',
      title: 'Sincronizar com o Google Agenda',
      summary: 'Veja como manter suas escalas no calendário pessoal, quando a integração estiver habilitada.',
      sections: [
        {
          title: 'Passo a passo',
          steps: [
            'Abra Calendário e escolha a opção de conectar ao Google Agenda.',
            'Autorize o acesso solicitado pela integração.',
            'Selecione o calendário de destino, quando essa opção for apresentada.',
            'Revise os eventos criados no Google Agenda.'
          ]
        },
        {
          title: 'Atenção',
          paragraphs: ['A integração é opcional. Se você desconectar o Google Agenda, as novas alterações deixarão de ser sincronizadas.']
        }
      ]
    }
  ];

  artigos: WikiArticle[] = [];
  artigosFiltrados: WikiArticle[] = [];
  artigoSelecionado: WikiArticle | null = null;
  contexto: 'gestao' | 'voluntario' = 'gestao';
  busca = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const contextoDaRota = this.route.snapshot.data['contexto'];
    const estaNoPortal = this.router.url.startsWith('/voluntario/');
    this.contexto = estaNoPortal || contextoDaRota === 'voluntario' ? 'voluntario' : 'gestao';
    this.artigos = this.contexto === 'voluntario' ? this.artigosVoluntario : this.artigosGestao;
    this.artigosFiltrados = [...this.artigos];
  }

  filtrar(): void {
    const termo = this.busca.trim().toLocaleLowerCase();
    if (!termo) {
      this.artigosFiltrados = [...this.artigos];
      return;
    }

    this.artigosFiltrados = this.artigos.filter((artigo) =>
      `${artigo.category} ${artigo.title} ${artigo.summary}`.toLocaleLowerCase().includes(termo)
    );
  }

  selecionarArtigo(artigo: WikiArticle): void {
    this.artigoSelecionado = artigo;
  }

  limparSelecao(): void {
    this.artigoSelecionado = null;
  }
}
