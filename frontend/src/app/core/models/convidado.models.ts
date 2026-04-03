export interface Convidado {
  id: number;
  cultoId: number;
  nome: string;
  telefone: string | null;
  quemConvidou: string | null;
  primeiraVezIgreja: boolean;
  observacoes: string | null;
  statusAcompanhamento: string;
}
