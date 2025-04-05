using CMS.Alportech.Cliente.VictoriaCaroline.Models;
using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class DashboardController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;

        public DashboardController(GoogleSheetsService googleSheetsService)
        {
            _googleSheetsService = googleSheetsService;
        }

        public async Task<IActionResult> Index()
        {
            var usuarioLogadoJson = HttpContext.Session.GetString("UsuarioLogado");

            if (string.IsNullOrEmpty(usuarioLogadoJson))
            {
                var usuarioId = Request.Cookies["UsuarioId"];
                if (!string.IsNullOrEmpty(usuarioId))
                {
                    // Busca o usuário pelo ID
                    var usuarios = await _googleSheetsService.ObterUsuarios();
                    var usuario = usuarios.FirstOrDefault(u => u.IdUsuario!.ToString() == usuarioId);
                    if (usuario != null)
                    {
                        usuarioLogadoJson = JsonConvert.SerializeObject(usuario);
                        HttpContext.Session.SetString("UsuarioLogado", usuarioLogadoJson);
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

            // Desserializa o usuário logado
            var usuarioLogado = JsonConvert.DeserializeObject<Usuario>(usuarioLogadoJson);
            var usuarioIdInt = usuarioLogado!.IdUsuario;

            // Obter contagens filtradas pelo IdUsuario
            var projetos = await _googleSheetsService.ObterDadosDaAba<Projeto>("Projetos");
            var experiencias = await _googleSheetsService.ObterDadosDaAba<Experiencia>("Experiencias");
            var formacoes = await _googleSheetsService.ObterDadosDaAba<Formacao>("Formacoes");
            var habilidades = await _googleSheetsService.ObterDadosDaAba<Conquista>("Conquistas");

            // Filtrar pelos projetos do usuário logado
            ViewBag.ProjetosCount = projetos?.Count(p => p.IdUsuario == usuarioIdInt) ?? 0;
            ViewBag.ExperienciasCount = experiencias?.Count(e => e.IdUsuario == usuarioIdInt) ?? 0;
            ViewBag.FormacoesCount = formacoes?.Count(f => f.IdUsuario == usuarioIdInt) ?? 0;
            ViewBag.ConquistasCount = habilidades?.Count(h => h.IdUsuario == usuarioIdInt) ?? 0;

            return View("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> GetCounts()
        {
            var usuarioLogadoJson = HttpContext.Session.GetString("UsuarioLogado");
            if (string.IsNullOrEmpty(usuarioLogadoJson))
            {
                return Json(new { success = false, message = "Usuário não logado" });
            }

            var usuarioLogado = JsonConvert.DeserializeObject<Usuario>(usuarioLogadoJson);
            var usuarioIdInt = usuarioLogado!.IdUsuario;

            try
            {
                var projetos = await _googleSheetsService.ObterDadosDaAba<Projeto>("Projetos");
                var experiencias = await _googleSheetsService.ObterDadosDaAba<Experiencia>("Experiencias");
                var formacoes = await _googleSheetsService.ObterDadosDaAba<Formacao>("Formacoes");
                var conquistas = await _googleSheetsService.ObterDadosDaAba<Conquista>("Conquistas");

                return Json(new
                {
                    success = true,
                    projectsCount = projetos?.Count(p => p.IdUsuario == usuarioIdInt) ?? 0,
                    experienceCount = experiencias?.Count(e => e.IdUsuario == usuarioIdInt) ?? 0,
                    educationCount = formacoes?.Count(f => f.IdUsuario == usuarioIdInt) ?? 0,
                    conquistaCount = conquistas?.Count(h => h.IdUsuario == usuarioIdInt) ?? 0
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}