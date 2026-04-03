namespace GestaoCulto.Domain.Entities
{
    public class UsuarioPerfil
    {
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public long PerfilId { get; set; }
        public Perfil Perfil { get; set; } = null!;
    }
}
