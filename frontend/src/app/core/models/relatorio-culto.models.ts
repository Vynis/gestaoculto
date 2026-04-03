export interface RelatorioCultoResumoEquipe {
  nome: string;
  total: number;
}

export interface RelatorioCultoResumo {
  totalEtapas: number;
  totalEscalados: number;
  totalConfirmados: number;
  totalPendentes: number;
  equipes: RelatorioCultoResumoEquipe[];
}

export interface RelatorioCultoInfo {
  id: number;
  nome: string;
  tipoCulto: string;
  dataCulto: string;
  horarioInicio: string;
  horarioFimPrevisto: string | null;
  observacoesGerais: string | null;
  totalVisitantes: number;
  totalNovosConvertidos: number;
  statusCultoId: number;
  statusCultoNome: string | null;
}

export interface RelatorioCultoEtapa {
  id: number;
  cultoId: number;
  sequencia: number;
  horarioInicio: string;
  horarioFimCalculado: string | null;
  duracaoMinutos: number;
  atividade: string;
  descricao: string | null;
  ministerioResponsavelId: number | null;
  ministerioResponsavelNome: string | null;
  statusEtapaId: number;
  statusEtapaNome: string | null;
  atrasoMinutos: number;
  acoesMinisterio: RelatorioCultoEtapaAcao[];
}

export interface RelatorioCultoEtapaAcao {
  id: number;
  etapaCultoId: number;
  ministerioId: number;
  ministerioNome: string | null;
  ordem: number | null;
  descricaoAcao: string;
  observacao: string | null;
  ativo: boolean;
}

export interface RelatorioCultoVoluntario {
  id: number;
  cultoId: number;
  etapaCultoId: number | null;
  funcao: string;
  voluntarioId: number;
  voluntarioNome: string | null;
  ministerioId: number | null;
  ministerioNome: string | null;
  presencaStatusId: number;
  presencaStatusNome: string | null;
  horarioPrevisto: string | null;
  confirmadoEm: string | null;
  observacoes: string | null;
}

export interface RelatorioCultoRepertorioItem {
  id: number;
  repertorioCultoId: number;
  musicaId: number;
  musicaTitulo: string | null;
  musicaTom?: string | null;
  tom?: string | null;
  musicaLinkCifra?: string | null;
  linkCifra?: string | null;
  musicaLinkVideo?: string | null;
  linkVideo?: string | null;
  musicaObservacoes?: string | null;
  etapaCultoId: number | null;
  etapaAtividade: string | null;
  ordem: number;
  responsavel: string | null;
  observacoes: string | null;
}

export interface RelatorioCultoRepertorio {
  id: number;
  cultoId: number;
  observacoes: string | null;
  itens: RelatorioCultoRepertorioItem[];
}

export interface RelatorioCultoItem {
  culto: RelatorioCultoInfo;
  resumo: RelatorioCultoResumo;
  cronograma: RelatorioCultoEtapa[];
  voluntarios: RelatorioCultoVoluntario[];
  repertorio: RelatorioCultoRepertorio;
}

export interface RelatorioCultoDiaResponse {
  data: string;
  quantidadeCultos: number;
  cultos: RelatorioCultoItem[];
}
