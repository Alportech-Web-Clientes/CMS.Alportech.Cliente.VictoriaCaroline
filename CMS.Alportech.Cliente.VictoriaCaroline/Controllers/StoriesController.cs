using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class StoriesController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public StoriesController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
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

            return View("Stories");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarStorie([FromBody] Story storie)
        {
            if (storie == null ||
                string.IsNullOrWhiteSpace(storie.LabelStorie) ||
                string.IsNullOrWhiteSpace(storie.TituloStorie) ||
                string.IsNullOrWhiteSpace(storie.DescricaoStorie) ||
                storie.ImagensStorieBase64 == null ||
                storie.ImagensStorieBase64.Count == 0)
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos e pelo menos uma imagem deve ser enviada." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            storie.IdUsuario = usuario?.IdUsuario!;
            storie.IdStorie = Guid.NewGuid().ToString();
            storie.DataCriacaoStorie = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var url = "https://script.google.com/macros/s/AKfycbxHfCb_e_zPcoky6RwuFAWGQSvgEioB-fHlA1n7qoaj1iH2UIj7_I0iXL537q9HYqGacg/exec";
            var content = new StringContent(JsonConvert.SerializeObject(storie), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Storie adicionado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao adicionar storie: " + (result?.message ?? "Erro desconhecido");
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterStories()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<Story> stories = await _googleSheetsService.ObterDadosDaAba<Story>("Stories");

            var storiesUsuario = stories
                .Where(p => p.IdUsuario == usuario!.IdUsuario)
                .OrderByDescending(p => p.DataCriacaoStorie)
                .ToList();

            return Json(new { success = true, stories = storiesUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> EditarStorie([FromBody] Story storie)
        {
            if (storie == null ||
                string.IsNullOrWhiteSpace(storie.IdStorie) ||
                string.IsNullOrWhiteSpace(storie.LabelStorie) ||
                string.IsNullOrWhiteSpace(storie.TituloStorie) ||
                string.IsNullOrWhiteSpace(storie.DescricaoStorie) ||
                storie.ImagensStorieBase64 == null ||
                storie.ImagensStorieBase64.Count == 0)
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos e pelo menos uma imagem deve ser mantida." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            storie.IdUsuario = usuario?.IdUsuario!;

            // Buscar o storie original para manter a data de criação
            var stories = await _googleSheetsService.ObterDadosDaAba<Story>("Stories");
            var storieOriginal = stories.FirstOrDefault(p => p.IdStorie == storie.IdStorie);

            if (storieOriginal == null)
            {
                return Json(new { success = false, message = "Storie não encontrado para edição." });
            }

            storie.DataCriacaoStorie = storieOriginal.DataCriacaoStorie;

            // 1 - Deleta storie antigo
            var deletarUrl = "https://script.google.com/macros/s/AKfycbygCDGKPLjLoYHuEzH51kYU0N9YuAqZLjQVOFkBQl21Pcl88EeSLSfiWl5ucAowegI82A/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdStorie = storie.IdStorie }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar storie original antes da edição." });
            }

            // 2 - Recria o storie com as novas informações
            var registrarUrl = "https://script.google.com/macros/s/AKfycbxHfCb_e_zPcoky6RwuFAWGQSvgEioB-fHlA1n7qoaj1iH2UIj7_I0iXL537q9HYqGacg/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(storie), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao registrar o storie atualizado." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Storie editado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao registrar storie atualizado.";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarStorie(string idStorie)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var stories = await _googleSheetsService.ObterDadosDaAba<Story>("Stories");
            var storie = stories.FirstOrDefault(p => p.IdStorie == idStorie && p.IdUsuario == usuario!.IdUsuario);

            if (storie == null)
            {
                return Json(new { success = false, message = "Storie não encontrado ou você não tem permissão para excluí-lo." });
            }

            var url = "https://script.google.com/macros/s/AKfycbygCDGKPLjLoYHuEzH51kYU0N9YuAqZLjQVOFkBQl21Pcl88EeSLSfiWl5ucAowegI82A/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new { IdStorie = idStorie }), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Storie excluído com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir storie.";
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