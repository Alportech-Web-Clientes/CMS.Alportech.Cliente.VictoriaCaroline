namespace CMS.Alportech.Cliente.VictoriaCaroline.Models
{
    public class Usuario
    {
        public string? IdUsuario { get; set; }
        public string? NomeUsuario { get; set; }
        public string? Email { get; set; }
        public string? SenhaHash { get; set; }
        public string? DataCadastro { get; set; }
        public string? FotoPerfil { get; set; }
    }
}