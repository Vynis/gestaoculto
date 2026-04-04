export interface RepertorioItem {
  id?: number;
  musicaId: number;
  musicaTitulo?: string;
  musicaArtistaBanda?: string | null;
  musicaTom?: string | null;
  musicaLinkCifra?: string | null;
  musicaLinkVideo?: string | null;
  musicaObservacoes?: string | null;
  etapaCultoId: number | null;
  etapaAtividade?: string | null;
  ordem: number;
  responsavel: string | null;
  observacoes: string | null;
}

export interface RepertorioCulto {
  id?: number;
  cultoId: number;
  observacoes: string | null;
  itens: RepertorioItem[];
}
