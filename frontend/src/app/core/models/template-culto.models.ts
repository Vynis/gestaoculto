export interface TemplateCulto {
  id: number;
  nome: string;
  tipoCulto: string;
  descricao: string | null;
  ativo: boolean;
  etapas: TemplateEtapa[];
}

export interface TemplateEtapaMinisterioAcao {
  id?: number;
  templateEtapaCultoId?: number;
  ministerioId: number;
  ministerioNome?: string | null;
  ordem: number | null;
  descricaoAcao: string;
  observacao: string | null;
  ativo: boolean;
}

export interface TemplateEtapa {
  id?: number;
  sequencia: number;
  horarioInicialPadrao: string | null;
  duracaoMinutos: number;
  atividade: string;
  descricao: string | null;
  ministerioResponsavelId: number | null;
  ministerioIds: number[];
  observacoes: string | null;
  statusEtapaId: number | null;
  acoesMinisterio: TemplateEtapaMinisterioAcao[];
}

export interface TemplateCultoRequest {
  nome: string;
  tipoCulto: string;
  descricao: string | null;
  ativo: boolean;
  etapas: TemplateEtapa[];
}
