export interface VoluntarioCompromisso {
  id: number;
  cultoId: number;
  cultoNome: string;
  dataCulto: string;
  horarioInicio: string;
  ministerioId: number | null;
  ministerioNome: string | null;
  funcao: string;
  etapaCultoId: number | null;
  etapaAtividade: string | null;
  etapaBlocoCronograma?: string | null;
  observacoes: string | null;
  presencaStatusId: number;
  presencaStatusNome: string | null;
}

export interface VoluntarioMinisterioResumo {
  ministerioId: number;
  ministerioNome: string;
  principal: boolean;
}

export interface VoluntarioPainelResponse {
  voluntario: {
    id: number;
    nome: string;
    email: string | null;
    telefone: string | null;
  };
  proximosCompromissos: VoluntarioCompromisso[];
  ministerios: VoluntarioMinisterioResumo[];
  totalProximosCompromissos: number;
  pendentesConfirmacao: number;
}

export interface VoluntarioCalendarioResponse {
  ano: number;
  mes: number;
  dias: Array<{
    data: string;
    total: number;
    itens: VoluntarioCompromisso[];
  }>;
}

export interface VoluntarioColegaMinisterio {
  voluntarioId: number;
  voluntarioNome: string;
  voluntarioTelefone: string | null;
  voluntarioEmail: string | null;
  ministerioId: number;
  ministerioNome: string;
  funcao?: string | null;
  ativo: boolean;
}

export interface VoluntarioMeusDados {
  id: number;
  nome: string;
  email: string | null;
  telefone: string | null;
  observacoes: string | null;
  restricoesIndisponibilidade: string | null;
}

export interface VoluntarioCultoPlanejado {
  id: number;
  nome: string;
  dataCulto: string;
  horarioInicio: string;
  statusCultoId: number;
  statusCultoNome: string;
  statusDisponibilidadeCodigo: string;
  statusDisponibilidadeNome: string;
  respondidoEm: string | null;
  observacao: string | null;
  ministerios: Array<{ ministerioId: number; ministerioNome: string }>;
  escalado: boolean;
  podeResponder: boolean;
}

export interface VoluntarioCultosPlanejadosResponse {
  ano: number;
  mes: number;
  cultos: VoluntarioCultoPlanejado[];
}

export interface VoluntarioDisponibilidadeCultoDetalhe {
  culto: {
    id: number;
    nome: string;
    dataCulto: string;
    horarioInicio: string;
  };
  podeResponder: boolean;
  ministeriosPermitidos: Array<{ ministerioId: number; ministerioNome: string; principal: boolean }>;
  disponibilidade: {
    statusDisponibilidadeId: number;
    statusCodigo: string;
    statusNome: string;
    observacao: string | null;
    respondidoEm: string;
    ministerioIds: number[];
  } | null;
}

export interface DisponibilidadeGestaoItem {
  id: number;
  cultoId: number;
  cultoNome: string;
  dataCulto: string;
  horarioInicio: string;
  voluntarioId: number;
  voluntarioNome: string;
  statusDisponibilidadeId: number;
  statusNome: string;
  observacao: string | null;
  respondidoEm: string;
  ministerios: Array<{ ministerioId: number; ministerioNome: string }>;
  escalado: boolean;
}

export interface StatusDisponibilidadeVoluntario {
  id: number;
  nome: string;
  codigo: string;
  corHex: string | null;
  ordem: number;
}

export interface VoluntarioRepertorioLouvorItem {
  id: number;
  ordem: number;
  musicaId: number;
  musicaTitulo: string | null;
  musicaArtistaBanda: string | null;
  musicaTom: string | null;
  musicaLinkCifra: string | null;
  musicaLinkVideo: string | null;
  musicaObservacoes: string | null;
  etapaCultoId: number | null;
  etapaAtividade: string | null;
  responsavel: string | null;
  observacoes: string | null;
}

export interface VoluntarioRepertorioLouvor {
  id: number;
  cultoId: number;
  observacoes: string | null;
  itens: VoluntarioRepertorioLouvorItem[];
}

export interface VoluntarioEscalaDetalheResponse {
  item: VoluntarioCompromisso;
  colegasMesmoMinisterio: VoluntarioColegaMinisterio[];
  repertorioLouvor: VoluntarioRepertorioLouvor | null;
}
