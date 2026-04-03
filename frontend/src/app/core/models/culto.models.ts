export interface Culto {
  id: number;
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
