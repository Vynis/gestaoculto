export interface EtapaMinisterioAcao {
  id?: number;
  etapaCultoId?: number;
  ministerioId: number;
  ministerioNome?: string | null;
  ordem: number | null;
  descricaoAcao: string;
  observacao: string | null;
  ativo: boolean;
}

export interface EtapaCulto {
  id: number;
  cultoId: number;
  sequencia: number;
  horarioInicio: string;
  duracaoMinutos: number;
  horarioFimCalculado: string | null;
  atividade: string;
  descricao: string | null;
  ministerioResponsavelId?: number | null;
  ministerioResponsavelNome?: string | null;
  atrasoMinutos: number;
  statusEtapaId: number;
  acoesMinisterio: EtapaMinisterioAcao[];
}
