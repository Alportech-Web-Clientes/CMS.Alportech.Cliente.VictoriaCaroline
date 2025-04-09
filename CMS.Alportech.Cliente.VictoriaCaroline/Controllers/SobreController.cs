using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class SobreController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public SobreController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
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
                    var usuarios = await _googleSheetsService.ObterUsuarios();
                    var usuario = usuarios.FirstOrDefault(u => u.IdUsuario!.ToString() == usuarioId);
                    if (usuario != null)
                    {
                        var usuarioJson = JsonConvert.SerializeObject(usuario);
                        HttpContext.Session.SetString("UsuarioLogado", usuarioJson);
                    }
                    else
                    {
                        return RedirectToAction("Login", "Auth");
                    }
                }
                else
                {
                    return RedirectToAction("Login", "Auth");
                }
            }

            return View("Sobre");
        }

        [HttpGet]
        public async Task<IActionResult> ObterInformacoesSobre()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            var informacoesSobre = await _googleSheetsService.ObterDadosDaAba<Sobre>("Sobre");
            var sobreUsuario = informacoesSobre.FirstOrDefault(s => s.IdUsuario == usuario!.IdUsuario);

            return Json(new { success = true, sobre = sobreUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> SalvarInformacoesSobre([FromBody] Sobre sobre)
        {
            if (sobre == null ||
                string.IsNullOrWhiteSpace(sobre.Nome) ||
                string.IsNullOrWhiteSpace(sobre.TituloOcupacao) ||
                string.IsNullOrWhiteSpace(sobre.DescricaoSobreMim))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            sobre.IdUsuario = usuario?.IdUsuario!;

            // Verifica se já existe registro para o usuário
            var informacoesSobre = await _googleSheetsService.ObterDadosDaAba<Sobre>("Sobre");
            var sobreExistente = informacoesSobre.FirstOrDefault(s => s.IdUsuario == sobre.IdUsuario);

            if (sobreExistente != null)
            {
                // 🔹 1 - Deleta registro antigo
                var deletarUrl = "https://script.google.com/macros/s/AKfycbxFftP4ekiCZvy5NKp2uLoMEK8qFezjsR6_h9YlmBb72VQP9Ia9FPgWOzmf-bkoD13PjA/exec";
                var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdUsuario = sobre.IdUsuario }), Encoding.UTF8, "application/json");
                var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Erro ao deletar informações antigas antes da atualização." });
                }
            }

            // 🔹 2 - Adiciona novo registro (com mesmo IdUsuario)
            var registrarUrl = "https://script.google.com/macros/s/AKfycbzbg9VDw0oqJNF2tZQGYMQNfCQTQoWBjZCL6mtxjFjp-2ZCLcm9fitO8V2c2m_KLaRWuw/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(sobre), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao salvar as novas informações." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Informações salvas com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao salvar informações.";
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