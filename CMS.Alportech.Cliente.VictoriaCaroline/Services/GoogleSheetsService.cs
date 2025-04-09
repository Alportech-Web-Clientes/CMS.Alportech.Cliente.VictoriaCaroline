using System.Globalization;
using System.Text.Json;
using System.Text;
using CsvHelper.Configuration;
using CsvHelper;
using CMS.Alportech.Cliente.VictoriaCaroline.Models;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Services
{
    public class GoogleSheetsService
    {
        private readonly HttpClient _httpClient;

        
        private readonly string _projetos = "https://docs.google.com/spreadsheets/d/1pfvcWvSELjRCuts9Aih_ydKvNsBQDGXG_b4gUNkZe5o/gviz/tq?tqx=out:csv&sheet=Projetos";
        private readonly string _urlAba = "https://docs.google.com/spreadsheets/d/1pfvcWvSELjRCuts9Aih_ydKvNsBQDGXG_b4gUNkZe5o/gviz/tq?tqx=out:csv&sheet=";
        private readonly string _atualizarSenhaUsuarioExec = "https://script.google.com/macros/s/AKfycbyi0276TQ8ULLuDy2PK_C7VSL6KcDTY7ELqjOOi8hqhQ4_dueoIQwRnDb66A5tESzpqpg/exec";
        private readonly string _criarUsuarioExec = "https://script.google.com/macros/s/AKfycbwNVUpsDqJ28cw-fcl2f6MOtxeby53-pYDvtcl0arIbrZ2ozX_sCwI8YpXhaRu31iG2iQ/exec";

        public GoogleSheetsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<List<T>> GetCsvDataAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            return csv.GetRecords<T>().ToList();
        }

        public async Task<List<Usuario>> ObterUsuarios()
        {
            var usuarios = await ObterDadosDaAba<Usuario>("Usuarios");
            return usuarios;
        }

        public async Task<List<T>> ObterDadosDaAba<T>(string aba)
        {
            var url = $"{_urlAba}{aba}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            //var rawCsv = await response.Content.ReadAsStringAsync();
            //Console.WriteLine($"CSV recebido:\n{rawCsv}");

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };

            using var csv = new CsvReader(reader, config);


            // Configuração específica para o tipo Story
            if (typeof(T) == typeof(Story))
            {
                csv.Context.RegisterClassMap<StoryMap>();
            }

            return csv.GetRecords<T>().ToList();
        }

        // Classe de mapeamento para Story
        public sealed class StoryMap : ClassMap<Story>
        {
            public StoryMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.ImagensStorieBase64).TypeConverter<StringToListConverter>();
            }
        }

        public async Task CriarUsuario(string nome, string email, string senha, string fotoPerfil)
        {
            var data = new
            {
                IdUsuario = Guid.NewGuid().ToString(),
                NomeUsuario = nome,
                Email = email,
                SenhaHash = senha,
                DataCadastro = DateTime.Now.ToString("yyyy-MM-dd"),
                FotoPerfil = fotoPerfil
            };

            var url = _criarUsuarioExec;
            var content = new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task AtualizarSenhaUsuario(string email, string novaSenha)
        {
            var data = new
            {
                Email = email,
                NovaSenha = novaSenha
            };

            var url = _atualizarSenhaUsuarioExec;
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Projeto>> ObterProjetos()
        {
            using var response = await _httpClient.GetAsync(_projetos);
            if (!response.IsSuccessStatusCode)
                return new List<Projeto>();

            var csvData = await response.Content.ReadAsStringAsync();
            using var reader = new StringReader(csvData);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ","
            });

            return [.. csv.GetRecords<Projeto>().OrderByDescending(p => DateTime.ParseExact(p.DataCriacaoProjeto!, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))];
        }
    }
}
