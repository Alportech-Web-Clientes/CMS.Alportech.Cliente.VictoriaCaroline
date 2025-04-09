using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class ExperienciasController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public ExperienciasController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
        {
            _googleSheetsService = googleSheetsService;
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");

            if (string.IsNullOrEmpty(usuarioLogado))
            {
                var usuarioId = Request.Cookies["UsuarioId"];
                if (!string.IsNullOrEmpty(usuarioId))
                {
                    // Busca o usuário pelo ID
                    var usuarios = await _googleSheetsService.ObterUsuarios();
                    var usuario = usuarios.FirstOrDefault(u => u.IdUsuario!.ToString() == usuarioId);
                    if (usuario != null)
                    {
                        var usuarioJson = JsonConvert.SerializeObject(usuario);
                        HttpContext.Session.SetString("UsuarioLogado", usuarioJson);
                    }
                    else
                    {
                        // Se não encontrar o usuário, redireciona para o login
                        return RedirectToAction("Login", "Auth");
                    }
                }
                else
                {
                    // Se não houver cookie de ID, redireciona para o login
                    return RedirectToAction("Login", "Auth");
                }
            }

            return View("Experiencias");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarExperiencia([FromBody] Experiencia experiencia)
        {
            if (experiencia == null ||
                string.IsNullOrWhiteSpace(experiencia.TituloExperiencia) ||
                string.IsNullOrWhiteSpace(experiencia.NomeEmpresa) ||
                string.IsNullOrWhiteSpace(experiencia.DataInicioExperiencia) ||
                string.IsNullOrWhiteSpace(experiencia.DescricaoExperiencia) ||
                string.IsNullOrWhiteSpace(experiencia.Jornada) ||
                string.IsNullOrWhiteSpace(experiencia.TrabalhoAtual)) // Novo campo obrigatório
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            experiencia.IdUsuario = usuario?.IdUsuario!;
            experiencia.IdExperiencia = Guid.NewGuid().ToString();
            experiencia.DataCriacaoExperiencia = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var url = "https://script.google.com/macros/s/AKfycbwMkb-exCVGLDB3uUBq5PrcPxF7yEV9-8a0-jR7GkOWKnguDdGm62A5uZvpPAtPKUsrSg/exec";
            var content = new StringContent(JsonConvert.SerializeObject(experiencia), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao comunicar com o serviço externo." });
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Experiência adicionada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao adicionar experiência: {mensagem}";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterExperiencias()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<Experiencia> experiencias = null!;
            experiencias = await _googleSheetsService.ObterDadosDaAba<Experiencia>("Experiencias");

            var experienciasUsuario = experiencias
                .Where(p => p.IdUsuario == usuario!.IdUsuario)
                .OrderByDescending(p => p.DataCriacaoExperiencia) // Ordenando as mais recentes primeiro
                .ToList();

            return Json(new { success = true, experiencias = experienciasUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> EditarExperiencia([FromBody] Experiencia experiencia)
        {
            if (experiencia == null ||
                string.IsNullOrWhiteSpace(experiencia.IdExperiencia) ||
                string.IsNullOrWhiteSpace(experiencia.TituloExperiencia) ||
                string.IsNullOrWhiteSpace(experiencia.NomeEmpresa) ||
                string.IsNullOrWhiteSpace(experiencia.Jornada) ||
                string.IsNullOrWhiteSpace(experiencia.DataInicioExperiencia) ||
                string.IsNullOrWhiteSpace(experiencia.DescricaoExperiencia) ||
                string.IsNullOrWhiteSpace(experiencia.TrabalhoAtual)) // Novo campo obrigatório
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }


            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            experiencia.IdUsuario = usuario?.IdUsuario!;

            // 🔹 Buscar a experiência original para manter a data de criação
            var experiencias = await _googleSheetsService.ObterDadosDaAba<Experiencia>("Experiencias");
            var experienciaOriginal = experiencias.FirstOrDefault(p => p.IdExperiencia == experiencia.IdExperiencia);

            if (experienciaOriginal == null)
            {
                return Json(new { success = false, message = "Experiência não encontrada para edição." });
            }

            experiencia.DataCriacaoExperiencia = experienciaOriginal.DataCriacaoExperiencia;

            // 🔹 Mantém a imagem original caso nenhuma nova seja enviada
            if (string.IsNullOrWhiteSpace(experiencia.ImagemExperienciaBase64))
            {
                experiencia.ImagemExperienciaBase64 = experienciaOriginal.ImagemExperienciaBase64;
            }

            // 🔸 1 - Deleta experiência antiga
            var deletarUrl = "https://script.google.com/macros/s/AKfycbwJzQiF6OQRQuTlV4jzyiIMfq_mFXixorsX5Rg24Yf3OsZW-YN3YkXRzfrkJIifxpE6WQ/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdExperiencia = experiencia.IdExperiencia }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar experiência original antes da edição." });
            }

            // 🔸 2 - Recria a experiência com as novas informações (usando mesmo IdExperiencia)
            var registrarUrl = "https://script.google.com/macros/s/AKfycbwMkb-exCVGLDB3uUBq5PrcPxF7yEV9-8a0-jR7GkOWKnguDdGm62A5uZvpPAtPKUsrSg/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(experiencia), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao registrar a experiência atualizada." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Experiência editada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao registrar experiência atualizada.";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarExperiencia(string idExperiencia)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var experiencias = await _googleSheetsService.ObterDadosDaAba<Experiencia>("Experiencias");
            var experiencia = experiencias.FirstOrDefault(p => p.IdExperiencia == idExperiencia && p.IdUsuario == usuario!.IdUsuario);

            if (experiencia == null)
            {
                return Json(new { success = false, message = "Experiência não encontrada ou você não tem permissão para excluí-la." });
            }

            var url = "https://script.google.com/macros/s/AKfycbwJzQiF6OQRQuTlV4jzyiIMfq_mFXixorsX5Rg24Yf3OsZW-YN3YkXRzfrkJIifxpE6WQ/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new { IdExperiencia = idExperiencia }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao comunicar com o serviço externo." });
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Experiência excluída com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir experiência.";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }
    }
}