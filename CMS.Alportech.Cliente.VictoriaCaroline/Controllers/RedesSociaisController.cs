using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class RedesSociaisController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public RedesSociaisController(GoogleSheetsService googleSheetsService,
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

            return View("RedesSociais");
        }

        [HttpGet]
        public async Task<IActionResult> ObterRedesSociais()
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);
            var redesSociais = await _googleSheetsService.ObterDadosDaAba<RedeSocial>("RedesSociais");
            var redesUsuario = redesSociais.Where(r => r.IdUsuario == usuario!.IdUsuario).ToList();

            return Json(new { success = true, redesSociais = redesUsuario });
        }

        [HttpPost]
        public async Task<IActionResult> SalvarRedesSociais([FromBody] List<RedeSocial> redesSociais)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            // Validações
            if (redesSociais == null || !redesSociais.Any())
            {
                return Json(new { success = false, message = "Nenhuma rede social enviada." });
            }

            // Verifica duplicatas
            var tipos = redesSociais.Select(r => r.TipoRedeSocial).Distinct();
            if (tipos.Count() != redesSociais.Count)
            {
                return Json(new { success = false, message = "Não é permitido ter redes sociais duplicadas." });
            }

            foreach (var rede in redesSociais)
            {
                if (string.IsNullOrWhiteSpace(rede.TipoRedeSocial) ||
                    string.IsNullOrWhiteSpace(rede.UrlRedeSocial) ||
                    !Uri.TryCreate(rede.UrlRedeSocial, UriKind.Absolute, out _))
                {
                    return Json(new { success = false, message = "Preencha todos os campos corretamente." });
                }
            }

            var usuario = JsonConvert.DeserializeObject<Usuario>(usuarioLogado);

            // Carrega imagens padrão
            foreach (var rede in redesSociais)
            {
                rede.IdUsuario = usuario!.IdUsuario;
                rede.IdRedeSocial = Guid.NewGuid().ToString();
                rede.NomeRedeSocial = ObterNomeAmigavel(rede.TipoRedeSocial!);
                rede.ImagemRedeSocialBase64 = await ObterImagemPadrao(rede.TipoRedeSocial!);
            }

            // 1. Deletar redes sociais existentes
            var deletarUrl = "https://script.google.com/macros/s/AKfycbwzzXEUHp-Hi0B2ia7PXrk4qQpuAFgllec1-dTow9-YkTMnaxXuOx3FAEVmuU8DRhQqxg/exec"; var deleteContent = new StringContent(
                JsonConvert.SerializeObject(new { IdUsuario = usuario!.IdUsuario }),
                Encoding.UTF8,
                "application/json");

            var deleteResponse = await _httpClient.PostAsync(deletarUrl, deleteContent);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao limpar redes sociais antigas." });
            }

            // 2. Adicionar novas redes sociais
            var registrarUrl = "https://script.google.com/macros/s/AKfycbxU-5XgSBiYaGmPM1xxfFgcEZqEBckieKfco2JcsurEmRZGtOgB-puCh14EPi7Ao0xa2w/exec";
            var createContent = new StringContent(
                JsonConvert.SerializeObject(redesSociais),
                Encoding.UTF8,
                "application/json");

            var createResponse = await _httpClient.PostAsync(registrarUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao salvar redes sociais." });
            }

            var responseContent = await createResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (result?.success == true)
            {
                TempData["SuccessMessage"] = "Redes sociais salvas com sucesso!";
                return Json(new { success = true });
            }

            return Json(new { success = false, message = result?.message ?? "Erro desconhecido" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeletarRedeSocial(string idRedeSocial)
        {
            var usuarioLogado = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                return Json(new { success = false, message = "Usuário não autenticado." });
            }

            var url = "https://script.google.com/macros/s/AKfycbyXz1ZjRBn3yleKzKHlnTVQ_E02AtyP3v15gFcCc4YiA-a8cNwIkB63K2rA9JxhCacVmw/exec";
            var content = new StringContent(
                JsonConvert.SerializeObject(new { IdRedeSocial = idRedeSocial }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Erro ao deletar rede social." });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (result?.success == true)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = result?.message ?? "Erro ao deletar rede social." });
        }

        private string ObterNomeAmigavel(string tipoRedeSocial)
        {
            return tipoRedeSocial switch
            {
                "email" => "Email",
                "linkedin" => "LinkedIn",
                "facebook" => "Facebook",
                "instagram" => "Instagram",
                "twitter" => "X (Twitter)",
                "youtube" => "YouTube",
                "tiktok" => "TikTok",
                _ => tipoRedeSocial
            };
        }

        private async Task<string> ObterImagemPadrao(string tipoRedeSocial)
        {
            var imagePath = tipoRedeSocial switch
            {
                "email" => "default_rede_email.png",
                "linkedin" => "default_rede_linkedin.png",
                "facebook" => "default_rede_facebook.png",
                "instagram" => "default_rede_instagram.png",
                "twitter" => "default_rede_x.png",
                "youtube" => "default_rede_youtube.png",
                "tiktok" => "default_rede_tiktok.png",
                _ => "default_rede_social.png"
            };

            var fullPath = Path.Combine(_hostingEnvironment.WebRootPath, "images", imagePath);
            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return Convert.ToBase64String(bytes);
        }
    }
}