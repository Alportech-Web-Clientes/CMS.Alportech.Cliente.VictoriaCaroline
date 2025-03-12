using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class ConquistasController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;

        public ConquistasController(GoogleSheetsService googleSheetsService)
        {
            _googleSheetsService = googleSheetsService;
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

            return View("Conquistas");
        }
    }
}