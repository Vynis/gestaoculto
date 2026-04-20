export interface Escala {
  id: number;
  cultoId: number;
  etapaCultoId: number | null;
  voluntarioId: number | null;
  voluntarioNome?: string | null;
  voluntarioAvulsoNome?: string | null;
  voluntarioAvulsoTelefone?: string | null;
  ministerioId: number | null;
  ministerioNome?: string | null;
  funcao: string;
  horarioPrevisto: string | null;
  presencaStatusId: number;
  presencaStatusNome?: string | null;
  confirmadoEm: string | null;
  observacoes: string | null;
}
