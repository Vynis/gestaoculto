export interface Musica {
  id: number;
  titulo: string;
  artistaBanda: string;
  tom: string | null;
  linkCifra: string | null;
  linkVideo: string | null;
  observacoes: string | null;
  ativo: boolean;
}

export interface MusicaRequest {
  titulo: string;
  artistaBanda: string;
  tom: string | null;
  linkCifra: string | null;
  linkVideo: string | null;
  observacoes: string | null;
  ativo: boolean;
}
