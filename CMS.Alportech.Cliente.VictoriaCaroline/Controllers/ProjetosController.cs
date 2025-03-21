using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class ProjetosController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public ProjetosController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
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

            return View("Projetos");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarProjeto([FromBody] Projeto projeto)
        {
            if (projeto == null ||
                string.IsNullOrWhiteSpace(projeto.TituloProjeto) ||
                string.IsNullOrWhiteSpace(projeto.DescricaoProjeto) ||
                string.IsNullOrWhiteSpace(projeto.TagsProjeto) ||
                string.IsNullOrWhiteSpace(projeto.ObjetivosProjeto) ||
                string.IsNullOrWhiteSpace(projeto.ResultadosProjeto))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            projeto.IdUsuario = usuario?.IdUsuario!;
            projeto.IdProjeto = Guid.NewGuid().ToString();
            projeto.DataCriacaoProjeto = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var url = "https://script.google.com/macros/s/AKfycbyk0FFkgNNbJxc26_TSUdStuPeRqzIK-s5TEvIwTJmMVq8cpZ1DSPnKeiLGqv56k6YXHw/exec";
            var content = new StringContent(JsonConvert.SerializeObject(projeto), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Projeto adicionado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    string mensagem = result?.message ?? "Erro desconhecido";
                    TempData["ErrorMessage"] = $"Erro ao adicionar projeto: {mensagem}";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido." });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterProjetos()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            List<Projeto> projetos = null!;
            projetos = await _googleSheetsService.ObterDadosDaAba<Projeto>("Projetos");

            var projetosUsuario = projetos
                .Where(p => p.IdUsuario == usuario!.IdUsuario)
                .OrderByDescending(p => p.DataCriacaoProjeto) // Ordenando os mais recentes primeiro
                .ToList();

            return Json(new { success = true, projetos = projetosUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> EditarProjeto([FromBody] Projeto projeto)
        {
            if (projeto == null ||
                string.IsNullOrWhiteSpace(projeto.IdProjeto) ||
                string.IsNullOrWhiteSpace(projeto.TituloProjeto) ||
                string.IsNullOrWhiteSpace(projeto.DescricaoProjeto) ||
                string.IsNullOrWhiteSpace(projeto.TagsProjeto) ||
                string.IsNullOrWhiteSpace(projeto.ObjetivosProjeto) ||
                string.IsNullOrWhiteSpace(projeto.ResultadosProjeto))
            {
                return Json(new { success = false, message = "Todos os campos obrigatórios devem ser preenchidos." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            projeto.IdUsuario = usuario?.IdUsuario!;

            // 🔹 Buscar o projeto original para manter a data de criação
            var projetos = await _googleSheetsService.ObterDadosDaAba<Projeto>("Projetos");
            var projetoOriginal = projetos.FirstOrDefault(p => p.IdProjeto == projeto.IdProjeto);

            if (projetoOriginal == null)
            {
                return Json(new { success = false, message = "Projeto não encontrado para edição." });
            }

            projeto.DataCriacaoProjeto = projetoOriginal.DataCriacaoProjeto;

            // 🔹 Mantém a imagem original caso nenhuma nova seja enviada
            if (string.IsNullOrWhiteSpace(projeto.ImagemProjetoBase64))
            {
                projeto.ImagemProjetoBase64 = projetoOriginal.ImagemProjetoBase64;
            }

            // 🔸 1 - Deleta projeto antigo
            var deletarUrl = "https://script.google.com/macros/s/AKfycbxVSD2FEymOAeyPOiTS9ljQSOM5bC3PVjTn8E-cfo2kdNIe31NTdSK3JWGHa4041afx/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdProjeto = projeto.IdProjeto }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar projeto original antes da edição." });
            }

            // 🔸 2 - Recria o projeto com as novas informações (usando mesmo IdProjeto)
            var registrarUrl = "https://script.google.com/macros/s/AKfycbyk0FFkgNNbJxc26_TSUdStuPeRqzIK-s5TEvIwTJmMVq8cpZ1DSPnKeiLGqv56k6YXHw/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(projeto), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao registrar o projeto atualizado." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);
                if (result?.success == true)
                {
                    TempData["SuccessMessage"] = "Projeto editado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    string mensagem = result?.message ?? "Erro desconhecido";
                    TempData["ErrorMessage"] = $"Erro ao registrar projeto atualizado: {mensagem}";
                    return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Erro ao parsear a resposta: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarProjeto(string idProjeto)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var projetos = await _googleSheetsService.ObterDadosDaAba<Projeto>("Projetos");
            var projeto = projetos.FirstOrDefault(p => p.IdProjeto == idProjeto && p.IdUsuario == usuario!.IdUsuario);

            if (projeto == null)
            {
                return Json(new { success = false, message = "Projeto não encontrado ou você não tem permissão para excluí-lo." });
            }

            var url = "https://script.google.com/macros/s/AKfycbxVSD2FEymOAeyPOiTS9ljQSOM5bC3PVjTn8E-cfo2kdNIe31NTdSK3JWGHa4041afx/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new { IdProjeto = idProjeto }), Encoding.UTF8, "application/json");
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
                    TempData["SuccessMessage"] = "Projeto excluído com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    string mensagem = result?.message ?? "Erro desconhecido";
                    TempData["ErrorMessage"] = $"Erro ao excluir projeto: {mensagem}";
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