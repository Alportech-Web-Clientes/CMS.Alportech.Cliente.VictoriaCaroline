using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class ContatoController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public ContatoController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
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

            return View("Contato");
        }

        [HttpGet]
        public async Task<IActionResult> ObterContato()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<Contato> contatos = await _googleSheetsService.ObterDadosDaAba<Contato>("Contato");

            var contatoUsuario = contatos.FirstOrDefault(p => p.IdUsuario == usuario!.IdUsuario);

            return Json(new { success = true, contato = contatoUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> AtualizarContato([FromBody] Contato contato)
        {
            if (contato == null || string.IsNullOrWhiteSpace(contato.Email))
            {
                return Json(new { success = false, message = "O campo Email é obrigatório." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            contato.IdUsuario = usuario?.IdUsuario!;

            var url = "https://script.google.com/macros/s/AKfycbwaAKEHm-27UkVL_Z8Og9wBTa2oEuPhnwHGmhfNzKAWqnblRP012iEm1r1VcMw8-Kye4w/exec";
            var content = new StringContent(JsonConvert.SerializeObject(contato), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Contato atualizado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao atualizar contato: " + (result?.message ?? "Erro desconhecido");
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