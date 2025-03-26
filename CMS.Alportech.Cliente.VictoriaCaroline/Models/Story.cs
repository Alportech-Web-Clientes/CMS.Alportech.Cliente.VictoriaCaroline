namespace CMS.Alportech.Cliente.VictoriaCaroline.Models
{
    public class Story
    {
        public string? IdStorie { get; set; }
        public string? IdUsuario { get; set; }
        public string? LabelStorie { get; set; }
        public string? TituloStorie { get; set; }
        public string? DescricaoStorie { get; set; }
        public List<string>? ImagensStorieBase64 { get; set; }
        public string? DataCriacaoStorie { get; set; }
    }
}