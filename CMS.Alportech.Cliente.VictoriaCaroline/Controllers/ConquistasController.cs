using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class ConquistasController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public ConquistasController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
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

            return View("Conquistas");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarConquista([FromBody] Conquista conquista)
        {
            if (conquista == null ||
                string.IsNullOrWhiteSpace(conquista.TituloConquista) ||
                string.IsNullOrWhiteSpace(conquista.NomeEmpresaConcedente) ||
                string.IsNullOrWhiteSpace(conquista.DataConquista) ||
                string.IsNullOrWhiteSpace(conquista.DescricaoConquista))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            conquista.IdUsuario = usuario?.IdUsuario!;
            conquista.IdConquista = Guid.NewGuid().ToString();
            conquista.DataCriacaoConquista = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var url = "https://script.google.com/macros/s/AKfycbzyupbbYL_6zKP4E64xOFPOV9NK8IDORopJeI_8Rrpi5bxZZ4gLX3kLp8sTF7ERgRD4WA/exec";
            var content = new StringContent(JsonConvert.SerializeObject(conquista), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Conquista adicionada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao adicionar conquista: " + (result?.message ?? "Erro desconhecido");
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterConquistas()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<Conquista> conquistas = await _googleSheetsService.ObterDadosDaAba<Conquista>("Conquistas");

            var conquistasUsuario = conquistas
                .Where(p => p.IdUsuario == usuario!.IdUsuario)
                .OrderByDescending(p => p.DataCriacaoConquista)
                .ToList();

            return Json(new { success = true, conquistas = conquistasUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> EditarConquista([FromBody] Conquista conquista)
        {
            if (conquista == null ||
                string.IsNullOrWhiteSpace(conquista.IdConquista) ||
                string.IsNullOrWhiteSpace(conquista.TituloConquista) ||
                string.IsNullOrWhiteSpace(conquista.NomeEmpresaConcedente) ||
                string.IsNullOrWhiteSpace(conquista.DataConquista) ||
                string.IsNullOrWhiteSpace(conquista.DescricaoConquista))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            conquista.IdUsuario = usuario?.IdUsuario!;

            // Buscar a conquista original para manter a data de criação
            var conquistas = await _googleSheetsService.ObterDadosDaAba<Conquista>("Conquistas");
            var conquistaOriginal = conquistas.FirstOrDefault(p => p.IdConquista == conquista.IdConquista);

            if (conquistaOriginal == null)
            {
                return Json(new { success = false, message = "Conquista não encontrada para edição." });
            }

            conquista.DataCriacaoConquista = conquistaOriginal.DataCriacaoConquista;

            // Mantém a imagem original caso nenhuma nova seja enviada
            if (string.IsNullOrWhiteSpace(conquista.ImagemConquistaBase64))
            {
                conquista.ImagemConquistaBase64 = conquistaOriginal.ImagemConquistaBase64;
            }

            // 1 - Deleta conquista antiga
            var deletarUrl = "https://script.google.com/macros/s/AKfycbxXxQB3e1GVfBVcGFJt3Opg3gyBO-Oky5SerWonh4180iLr-PGRh_j-keatDeLvGnZf4Q/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdConquista = conquista.IdConquista }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar conquista original antes da edição." });
            }

            // 2 - Recria a conquista com as novas informações
            var registrarUrl = "https://script.google.com/macros/s/AKfycbzyupbbYL_6zKP4E64xOFPOV9NK8IDORopJeI_8Rrpi5bxZZ4gLX3kLp8sTF7ERgRD4WA/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(conquista), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao registrar a conquista atualizada." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Conquista editada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao registrar conquista atualizada.";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarConquista(string idConquista)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var conquistas = await _googleSheetsService.ObterDadosDaAba<Conquista>("Conquistas");
            var conquista = conquistas.FirstOrDefault(p => p.IdConquista == idConquista && p.IdUsuario == usuario!.IdUsuario);

            if (conquista == null)
            {
                return Json(new { success = false, message = "Conquista não encontrada ou você não tem permissão para excluí-la." });
            }

            var url = "https://script.google.com/macros/s/AKfycbxXxQB3e1GVfBVcGFJt3Opg3gyBO-Oky5SerWonh4180iLr-PGRh_j-keatDeLvGnZf4Q/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new { IdConquista = idConquista }), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Conquista excluída com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir conquista.";
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