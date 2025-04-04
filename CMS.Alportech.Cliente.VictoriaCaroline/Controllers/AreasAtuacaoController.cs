using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class AreasAtuacaoController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AreasAtuacaoController(GoogleSheetsService googleSheetsService,
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

            return View("AreasAtuacao");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarAreaAtuacao([FromBody] AreaAtuacao areaAtuacao)
        {
            if (areaAtuacao == null ||
                string.IsNullOrWhiteSpace(areaAtuacao.TituloAreaAtuacao) ||
                string.IsNullOrWhiteSpace(areaAtuacao.DescricaoAreaAtuacao))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            areaAtuacao.IdUsuario = usuario?.IdUsuario!;
            areaAtuacao.IdAreaAtuacao = Guid.NewGuid().ToString();
            areaAtuacao.DataCriacaoAreaAtuacao = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var url = "https://script.google.com/macros/s/AKfycbx94QWjkFgfVCPnHoUVlCco63kP-GR45vpf_by-ywJx2q23WQ80AiWI0QtnFf6gBt6hNQ/exec";
            var content = new StringContent(JsonConvert.SerializeObject(areaAtuacao), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Área de atuação adicionada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao adicionar área de atuação: " + (result?.message ?? "Erro desconhecido");
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterAreasAtuacao()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<AreaAtuacao> areasAtuacao = await _googleSheetsService.ObterDadosDaAba<AreaAtuacao>("AreasAtuacao");

            var areasAtuacaoUsuario = areasAtuacao
                .Where(p => p.IdUsuario == usuario!.IdUsuario)
                .OrderByDescending(p => p.DataCriacaoAreaAtuacao)
                .ToList();

            return Json(new { success = true, areasAtuacao = areasAtuacaoUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> EditarAreaAtuacao([FromBody] AreaAtuacao areaAtuacao)
        {
            if (areaAtuacao == null ||
                string.IsNullOrWhiteSpace(areaAtuacao.IdAreaAtuacao) ||
                string.IsNullOrWhiteSpace(areaAtuacao.TituloAreaAtuacao) ||
                string.IsNullOrWhiteSpace(areaAtuacao.DescricaoAreaAtuacao))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            areaAtuacao.IdUsuario = usuario?.IdUsuario!;

            // Buscar a área de atuação original para manter a data de criação
            var areasAtuacao = await _googleSheetsService.ObterDadosDaAba<AreaAtuacao>("AreasAtuacao");
            var areaAtuacaoOriginal = areasAtuacao.FirstOrDefault(p => p.IdAreaAtuacao == areaAtuacao.IdAreaAtuacao);

            if (areaAtuacaoOriginal == null)
            {
                return Json(new { success = false, message = "Área de atuação não encontrada para edição." });
            }

            areaAtuacao.DataCriacaoAreaAtuacao = areaAtuacaoOriginal.DataCriacaoAreaAtuacao;

            // Mantém a imagem original caso nenhuma nova seja enviada
            if (string.IsNullOrWhiteSpace(areaAtuacao.ImagemAreaAtuacaoBase64))
            {
                areaAtuacao.ImagemAreaAtuacaoBase64 = areaAtuacaoOriginal.ImagemAreaAtuacaoBase64;
            }

            // 1 - Deleta área de atuação antiga
            var deletarUrl = "https://script.google.com/macros/s/AKfycbyg40xiTDXiMDiO2Y72DXDVNgHa8iVBev6K3oXRgIMIt3udcbGDymUaOU_yg46ML0Nw/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdAreaAtuacao = areaAtuacao.IdAreaAtuacao }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar área de atuação original antes da edição." });
            }

            // 2 - Recria a área de atuação com as novas informações
            var registrarUrl = "https://script.google.com/macros/s/AKfycbx94QWjkFgfVCPnHoUVlCco63kP-GR45vpf_by-ywJx2q23WQ80AiWI0QtnFf6gBt6hNQ/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(areaAtuacao), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao registrar a área de atuação atualizada." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Área de atuação editada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao registrar área de atuação atualizada.";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarAreaAtuacao(string idAreaAtuacao)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var areasAtuacao = await _googleSheetsService.ObterDadosDaAba<AreaAtuacao>("AreasAtuacao");
            var areaAtuacao = areasAtuacao.FirstOrDefault(p => p.IdAreaAtuacao == idAreaAtuacao && p.IdUsuario == usuario!.IdUsuario);

            if (areaAtuacao == null)
            {
                return Json(new { success = false, message = "Área de atuação não encontrada ou você não tem permissão para excluí-la." });
            }

            var url = "https://script.google.com/macros/s/AKfycbyg40xiTDXiMDiO2Y72DXDVNgHa8iVBev6K3oXRgIMIt3udcbGDymUaOU_yg46ML0Nw/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new { IdAreaAtuacao = idAreaAtuacao }), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Área de atuação excluída com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir área de atuação.";
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