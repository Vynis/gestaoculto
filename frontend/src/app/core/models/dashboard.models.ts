export interface DashboardResumo {
  proximoCultoId: number | null;
  proximoCultoNome: string | null;
  voluntariosEscalados: number;
  tarefasPendentes: number;
  convidadosRegistrados: number;
  novosConvertidos: number;
  alertasOperacionais: number;
}

export interface DashboardVoluntarioRanking {
  voluntarioId: number;
  nome: string;
  quantidadeCultos: number;
  ativo: boolean;
}

export interface DashboardMusicaRanking {
  musicaId: number;
  titulo: string;
  artistaBanda: string;
  quantidadeCultos: number;
  ativo: boolean;
}

export interface DashboardEstatisticas {
  periodoInicio: string;
  periodoFim: string;
  voluntarios: DashboardVoluntarioRanking[];
  musicas: DashboardMusicaRanking[];
}
