using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class DestaquesController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public DestaquesController(GoogleSheetsService googleSheetsService,
                                HttpClient httpClient,
                                IWebHostEnvironment hostingEnvironment)
        {
            _googleSheetsService = googleSheetsService;
            _httpClient = httpClient;
            _hostingEnvironment = hostingEnvironment;
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

            return View("Destaques");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarDestaque([FromBody] Destaque destaque)
        {
            if (destaque == null ||
                string.IsNullOrWhiteSpace(destaque.TituloDestaque) ||
                string.IsNullOrWhiteSpace(destaque.DescricaoDestaque))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            destaque.IdUsuario = usuario?.IdUsuario!;
            destaque.IdDestaque = Guid.NewGuid().ToString();
            destaque.DataCriacaoDestaque = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var url = "https://script.google.com/macros/s/AKfycbxDKDjw0pTVcSua-WKcnEB0m66as93QnH5nCg50BPxedd8lbL-rgFK-GEO-v_ng-9g6Bg/exec";
            var content = new StringContent(JsonConvert.SerializeObject(destaque), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Destaque adicionado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao adicionar destaque: " + (result?.message ?? "Erro desconhecido");
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterDestaques()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<Destaque> destaques = await _googleSheetsService.ObterDadosDaAba<Destaque>("Destaques");

            var destaquesUsuario = destaques
                .Where(p => p.IdUsuario == usuario!.IdUsuario)
                .OrderByDescending(p => p.DataCriacaoDestaque)
                .ToList();

            return Json(new { success = true, destaques = destaquesUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> EditarDestaque([FromBody] Destaque destaque)
        {
            if (destaque == null ||
                string.IsNullOrWhiteSpace(destaque.IdDestaque) ||
                string.IsNullOrWhiteSpace(destaque.TituloDestaque) ||
                string.IsNullOrWhiteSpace(destaque.DescricaoDestaque))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            destaque.IdUsuario = usuario?.IdUsuario!;

            var destaques = await _googleSheetsService.ObterDadosDaAba<Destaque>("Destaques");
            var destaqueOriginal = destaques.FirstOrDefault(p => p.IdDestaque == destaque.IdDestaque);

            if (destaqueOriginal == null)
            {
                return Json(new { success = false, message = "Destaque não encontrado para edição." });
            }

            destaque.DataCriacaoDestaque = destaqueOriginal.DataCriacaoDestaque;

            if (string.IsNullOrWhiteSpace(destaque.ImagemDestaqueBase64))
            {
                destaque.ImagemDestaqueBase64 = destaqueOriginal.ImagemDestaqueBase64;
            }

            // 1 - Deleta destaque antigo
            var deletarUrl = "https://script.google.com/macros/s/AKfycbx6yn5J3JrG-nxmC5BXwqKZSnSHpBJqnV5scAVdrWlIdhAmiSn61Kuj7uNYSA0mojrPsw/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdDestaque = destaque.IdDestaque }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar destaque original antes da edição." });
            }

            // 2 - Recria o destaque com as novas informações
            var registrarUrl = "https://script.google.com/macros/s/AKfycbxDKDjw0pTVcSua-WKcnEB0m66as93QnH5nCg50BPxedd8lbL-rgFK-GEO-v_ng-9g6Bg/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(destaque), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao registrar o destaque atualizado." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Destaque editado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao registrar destaque atualizado.";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarDestaque(string idDestaque)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var destaques = await _googleSheetsService.ObterDadosDaAba<Destaque>("Destaques");
            var destaque = destaques.FirstOrDefault(p => p.IdDestaque == idDestaque && p.IdUsuario == usuario!.IdUsuario);

            if (destaque == null)
            {
                return Json(new { success = false, message = "Destaque não encontrado ou você não tem permissão para excluí-lo." });
            }

            var url = "https://script.google.com/macros/s/AKfycbx6yn5J3JrG-nxmC5BXwqKZSnSHpBJqnV5scAVdrWlIdhAmiSn61Kuj7uNYSA0mojrPsw/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new { IdDestaque = idDestaque }), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Destaque excluído com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir destaque.";
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