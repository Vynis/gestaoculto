export interface MinisterioLider {
  usuarioId: number;
  usuarioNome: string;
  principal: boolean;
}

export interface MinisterioVoluntario {
  voluntarioId: number;
  voluntarioNome: string;
  principal: boolean;
}

export interface MinisterioFuncaoPadrao {
  id?: number;
  ministerioId?: number;
  nome: string;
  ordem?: number;
  ativo: boolean;
}

export interface MinisterioCompleto {
  id: number;
  nome: string;
  codigo: string;
  descricao: string | null;
  ativo: boolean;
  lideres: MinisterioLider[];
  voluntarios: MinisterioVoluntario[];
  funcoesPadrao: MinisterioFuncaoPadrao[];
}

export interface MinisterioRequest {
  nome: string;
  descricao: string | null;
  ativo: boolean;
  lideres: { usuarioId: number; principal: boolean }[];
  voluntarioIds: number[];
  funcoesPadrao: MinisterioFuncaoPadrao[];
}

export interface MinisterioOpcoes {
  usuarios: { id: number; nome: string; email: string }[];
  voluntarios: { id: number; nome: string; email: string }[];
}
