using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class FormacoesController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public FormacoesController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
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

            return View("Formacoes");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarFormacao([FromBody] Formacao formacao)
        {
            if (formacao == null ||
                string.IsNullOrWhiteSpace(formacao.TituloFormacao) ||
                string.IsNullOrWhiteSpace(formacao.NomeInstituicao) ||
                string.IsNullOrWhiteSpace(formacao.DataInicioFormacao) ||
                string.IsNullOrWhiteSpace(formacao.DataFimFormacao))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            formacao.IdUsuario = usuario?.IdUsuario!;
            formacao.IdFormacao = Guid.NewGuid().ToString();
            formacao.DataCriacaoFormacao = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var url = "https://script.google.com/macros/s/AKfycbzt1Rrl7ouVFb36vEzAX5GJ2EzACap3x646qV8_2UGB3lWD0DsJx8H_qIZTicKPYPmN/exec";
            var content = new StringContent(JsonConvert.SerializeObject(formacao), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Formação adicionada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    string mensagem = result?.message ?? "Erro desconhecido";
                    TempData["ErrorMessage"] = "Erro ao adicionar formação: {mensagem}";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterFormacoes()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<Formacao> formacoes = null!;
            formacoes = await _googleSheetsService.ObterDadosDaAba<Formacao>("Formacoes");

            var formacoesUsuario = formacoes
                .Where(p => p.IdUsuario == usuario!.IdUsuario)
                .OrderByDescending(p => p.DataCriacaoFormacao) // Ordenando as mais recentes primeiro
                .ToList();

            return Json(new { success = true, formacoes = formacoesUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> EditarFormacao([FromBody] Formacao formacao)
        {
            if (formacao == null ||
                string.IsNullOrWhiteSpace(formacao.TituloFormacao) ||
                string.IsNullOrWhiteSpace(formacao.NomeInstituicao) ||
                string.IsNullOrWhiteSpace(formacao.DataInicioFormacao) ||
                string.IsNullOrWhiteSpace(formacao.DataFimFormacao))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            formacao.IdUsuario = usuario?.IdUsuario!;

            // 🔹 Buscar a formação original para manter a data de criação
            var formacoes = await _googleSheetsService.ObterDadosDaAba<Formacao>("Formacoes");
            var formacaoOriginal = formacoes.FirstOrDefault(p => p.IdFormacao == formacao.IdFormacao);

            if (formacaoOriginal == null)
            {
                return Json(new { success = false, message = "Formação não encontrada para edição." });
            }

            formacao.DataCriacaoFormacao = formacaoOriginal.DataCriacaoFormacao;

            // 🔹 Mantém a imagem original caso nenhuma nova seja enviada
            if (string.IsNullOrWhiteSpace(formacao.ImagemFormacaoBase64))
            {
                formacao.ImagemFormacaoBase64 = formacaoOriginal.ImagemFormacaoBase64;
            }

            // 🔸 1 - Deleta formação antiga
            var deletarUrl = "https://script.google.com/macros/s/AKfycbxrbnVdoVK2JLDeir3xqSFFi9eqBR71qjsPLzbrfzBKd2GN4a98HoOpnTwfnAm6Sfs6WQ/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdFormacao = formacao.IdFormacao }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar formação original antes da edição." });
            }

            // 🔸 2 - Recria a formação com as novas informações (usando mesmo IdFormacao)
            var registrarUrl = "https://script.google.com/macros/s/AKfycbzt1Rrl7ouVFb36vEzAX5GJ2EzACap3x646qV8_2UGB3lWD0DsJx8H_qIZTicKPYPmN/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(formacao), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao registrar a formação atualizada." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Formação editada com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao registrar formação atualizada.";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarFormacao(string idFormacao)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var formacoes = await _googleSheetsService.ObterDadosDaAba<Formacao>("Formacoes");
            var formacao = formacoes.FirstOrDefault(p => p.IdFormacao == idFormacao && p.IdUsuario == usuario!.IdUsuario);

            if (formacao == null)
            {
                return Json(new { success = false, message = "Formação não encontrada ou você não tem permissão para excluí-la." });
            }

            var url = "https://script.google.com/macros/s/AKfycbxrbnVdoVK2JLDeir3xqSFFi9eqBR71qjsPLzbrfzBKd2GN4a98HoOpnTwfnAm6Sfs6WQ/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new { IdFormacao = idFormacao }), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Formação excluída com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir formação.";
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