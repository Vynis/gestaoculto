export interface Culto {
  id: number;
  recorrenciaId?: number | null;
  nome: string;
  tipoCulto: string;
  dataCulto: string;
  horarioInicio: string;
  horarioFimPrevisto: string | null;
  statusCultoId: number;
  observacoesGerais: string | null;
  totalVisitantes: number;
  totalNovosConvertidos: number;
}

export interface CultoRecorrencia {
  id: number;
  nome: string;
  tipoCulto: string;
  diaSemana: number;
  horarioInicio: string;
  horarioFimPrevisto: string | null;
  statusCultoId: number;
  observacoesGerais: string | null;
  templateCultoId: number | null;
  templateCultoNome: string | null;
  quantidadeSemanasAntecedencia: number;
  ativo: boolean;
  ultimaGeracaoEm: string | null;
}

export interface CultoRecorrenciaRequest {
  nome: string;
  tipoCulto: string;
  diaSemana: number;
  horarioInicio: string;
  horarioFimPrevisto: string | null;
  statusCultoId: number;
  observacoesGerais: string | null;
  templateCultoId: number | null;
  quantidadeSemanasAntecedencia: number;
  ativo: boolean;
}

export interface CultoRecorrenciaGeracaoResponse {
  mensagem: string;
  criados: number;
  ignorados: number;
  cultoIdsCriados: number[];
}

export interface CultoRequest {
  nome: string;
  tipoCulto: string;
  dataCulto: string;
  horarioInicio: string;
  horarioFimPrevisto: string | null;
  statusCultoId: number;
  observacoesGerais: string | null;
  templateCultoId: number | null;
}
