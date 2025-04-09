using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class ConfiguracoesController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;

        public ConfiguracoesController(GoogleSheetsService googleSheetsService, HttpClient httpClient)
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

            var usuarioDeserializado = JsonConvert.DeserializeObject<Usuario>(usuarioLogado!);
            ViewBag.FotoPerfil = usuarioDeserializado?.FotoPerfil;
            ViewBag.NomeUsuario = usuarioDeserializado?.NomeUsuario;
            ViewBag.Email = usuarioDeserializado?.Email;

            return View("Configuracoes");
        }

        [HttpPost]
        public async Task<IActionResult> AtualizarNome([FromBody] AtualizarNomeModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NovoNome))
            {
                return Json(new { success = false, message = "O nome não pode estar vazio." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            // 1. Deletar o usuário atual
            var deletarUrl = "https://script.google.com/macros/s/AKfycbzTgS75Y8RQhf4QotfJ5QKy7FhwxTuHOuf1gI89Lb_jiiy-FZZCcz2Rui7LWDb-cjKouQ/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdUsuario = usuario?.IdUsuario }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar usuário antes da atualização." });
            }

            // 2. Atualizar o nome
            usuario!.NomeUsuario = model.NovoNome;

            // 3. Recriar o usuário com os dados atualizados
            var registrarUrl = "https://script.google.com/macros/s/AKfycbwNVUpsDqJ28cw-fcl2f6MOtxeby53-pYDvtcl0arIbrZ2ozX_sCwI8YpXhaRu31iG2iQ/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(usuario), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao recriar usuário com dados atualizados." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);

            if (result?.success == true)
            {
                HttpContext.Session.SetString("UsuarioLogado", JsonConvert.SerializeObject(usuario));
                TempData["SuccessMessage"] = "Nome atualizado com sucesso!";
                return Json(new { success = true, message = "Nome atualizado com sucesso!" });
            }

            TempData["ErrorMessage"] = result?.message ?? "Erro ao atualizar nome.";
            return Json(new { success = false, message = result?.message ?? "Erro ao atualizar nome." });
        }

        [HttpPost]
        public async Task<IActionResult> AtualizarSenha([FromBody] AtualizarSenhaModel model)
        {
            if (string.IsNullOrWhiteSpace(model.SenhaAtual) || string.IsNullOrWhiteSpace(model.NovaSenha) || string.IsNullOrWhiteSpace(model.ConfirmarSenha))
            {
                return Json(new { success = false, message = "Todos os campos são obrigatórios." });
            }

            if (model.NovaSenha != model.ConfirmarSenha)
            {
                return Json(new { success = false, message = "As senhas não coincidem." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            if (usuario?.SenhaHash != model.SenhaAtual)
            {
                return Json(new { success = false, message = "Senha atual incorreta." });
            }

            // 1. Deletar o usuário atual
            var deletarUrl = "https://script.google.com/macros/s/AKfycbzTgS75Y8RQhf4QotfJ5QKy7FhwxTuHOuf1gI89Lb_jiiy-FZZCcz2Rui7LWDb-cjKouQ/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdUsuario = usuario?.IdUsuario }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar usuário antes da atualização." });
            }

            // 2. Atualizar a senha
            usuario!.SenhaHash = model.NovaSenha;

            // 3. Recriar o usuário com os dados atualizados
            var registrarUrl = "https://script.google.com/macros/s/AKfycbwNVUpsDqJ28cw-fcl2f6MOtxeby53-pYDvtcl0arIbrZ2ozX_sCwI8YpXhaRu31iG2iQ/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(usuario), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao recriar usuário com dados atualizados." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);

            if (result?.success == true)
            {
                HttpContext.Session.SetString("UsuarioLogado", JsonConvert.SerializeObject(usuario));
                TempData["SuccessMessage"] = "Senha atualizada com sucesso!";
                return Json(new { success = true, message = "Senha atualizada com sucesso!" });
            }

            TempData["ErrorMessage"] = result?.message ?? "Erro ao atualizar senha.";
            return Json(new { success = false, message = result?.message ?? "Erro ao atualizar senha." });
        }

        [HttpPost]
        public async Task<IActionResult> AtualizarFotoPerfil([FromBody] AtualizarFotoModel model)
        {
            if (string.IsNullOrWhiteSpace(model.FotoBase64))
            {
                TempData["ErrorMessage"] = "Nenhuma imagem foi enviada.";
                return Json(new { success = false, message = "Nenhuma imagem foi enviada." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            // 1. Deletar o usuário atual
            var deletarUrl = "https://script.google.com/macros/s/AKfycbzTgS75Y8RQhf4QotfJ5QKy7FhwxTuHOuf1gI89Lb_jiiy-FZZCcz2Rui7LWDb-cjKouQ/exec";
            var deleteContent = new StringContent(JsonConvert.SerializeObject(new { IdUsuario = usuario?.IdUsuario }), Encoding.UTF8, "application/json");
            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar usuário antes da atualização." });
            }

            // 2. Atualizar a foto
            usuario!.FotoPerfil = model.FotoBase64;

            // 3. Recriar o usuário com os dados atualizados
            var registrarUrl = "https://script.google.com/macros/s/AKfycbwNVUpsDqJ28cw-fcl2f6MOtxeby53-pYDvtcl0arIbrZ2ozX_sCwI8YpXhaRu31iG2iQ/exec";
            var createContent = new StringContent(JsonConvert.SerializeObject(usuario), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Erro ao recriar usuário com dados atualizados.";
                return Json(new { success = false, message = "Erro ao recriar usuário com dados atualizados." });
            }

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(createResponseContent);

            if (result?.success == true)
            {
                HttpContext.Session.SetString("UsuarioLogado", JsonConvert.SerializeObject(usuario));
                TempData["SuccessMessage"] = "Foto de perfil atualizada com sucesso!";
                return Json(new
                {
                    success = true,
                    message = "Foto de perfil atualizada com sucesso!",
                    fotoUrl = $"data:image/png;base64,{model.FotoBase64}"
                });
            }

            TempData["ErrorMessage"] = result?.message ?? "Erro ao atualizar foto de perfil.";
            return Json(new { success = false, message = result?.message ?? "Erro ao atualizar foto de perfil." });
        }
    }

    public class AtualizarNomeModel
    {
        public string? NovoNome { get; set; }
    }

    public class AtualizarSenhaModel
    {
        public string? SenhaAtual { get; set; }
        public string? NovaSenha { get; set; }
        public string? ConfirmarSenha { get; set; }
    }

    public class AtualizarFotoModel
    {
        public string? FotoBase64 { get; set; }
    }
}