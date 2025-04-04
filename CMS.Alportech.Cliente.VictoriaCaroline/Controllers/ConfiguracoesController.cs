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

            var url = "https://script.google.com/macros/s/AKfycbyOBAlmZn_UloNDZjCEWyRxQ3mdGxYJ3HTqUWf00a9kvjyzaoebfwf9Kqe62-mvop3QGQ/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                IdUsuario = usuario?.IdUsuario,
                NomeUsuario = model.NovoNome,
                TipoAtualizacao = "nome"
            }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao comunicar com o serviço externo." });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (result?.success == true)
            {
                usuario!.NomeUsuario = model.NovoNome;
                HttpContext.Session.SetString("UsuarioLogado", JsonConvert.SerializeObject(usuario));
                return Json(new { success = true, message = "Nome atualizado com sucesso!" });
            }

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

            var url = "https://script.google.com/macros/s/AKfycbyOBAlmZn_UloNDZjCEWyRxQ3mdGxYJ3HTqUWf00a9kvjyzaoebfwf9Kqe62-mvop3QGQ/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                IdUsuario = usuario?.IdUsuario,
                SenhaHash = model.NovaSenha,
                TipoAtualizacao = "senha"
            }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao comunicar com o serviço externo." });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (result?.success == true)
            {
                usuario!.SenhaHash = model.NovaSenha;
                HttpContext.Session.SetString("UsuarioLogado", JsonConvert.SerializeObject(usuario));
                return Json(new { success = true, message = "Senha atualizada com sucesso!" });
            }

            return Json(new { success = false, message = result?.message ?? "Erro ao atualizar senha." });
        }

        [HttpPost]
        public async Task<IActionResult> AtualizarFotoPerfil([FromBody] AtualizarFotoModel model)
        {
            if (string.IsNullOrWhiteSpace(model.FotoBase64))
            {
                return Json(new { success = false, message = "Nenhuma imagem foi enviada." });
            }

            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            var url = "https://script.google.com/macros/s/AKfycbyOBAlmZn_UloNDZjCEWyRxQ3mdGxYJ3HTqUWf00a9kvjyzaoebfwf9Kqe62-mvop3QGQ/exec";
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                IdUsuario = usuario?.IdUsuario,
                FotoPerfil = model.FotoBase64,
                TipoAtualizacao = "foto"
            }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao comunicar com o serviço externo." });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (result?.success == true)
            {
                usuario!.FotoPerfil = model.FotoBase64;
                HttpContext.Session.SetString("UsuarioLogado", JsonConvert.SerializeObject(usuario));
                return Json(new { success = true, message = "Foto de perfil atualizada com sucesso!", fotoUrl = $"data:image/png;base64,{model.FotoBase64}" });
            }

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