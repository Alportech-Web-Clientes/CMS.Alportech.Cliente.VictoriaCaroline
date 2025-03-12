using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CMS.Alportech.Cliente.VictoriaCaroline.Controllers
{
    public class AuthController : Controller
    {
        private readonly GoogleSheetsService _googleSheetsService;

        public AuthController(GoogleSheetsService googleSheetsService)
        {
            _googleSheetsService = googleSheetsService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string senha, bool lembrarDeMim)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Error = "Email e senha são obrigatórios.";
                return View();
            }
            var usuarios = await _googleSheetsService.ObterUsuarios();
            var usuario = usuarios.FirstOrDefault(u => u.Email == email);

            if (usuario == null)
            {
                ViewBag.Error = "Usuário não cadastrado.";
                return View();
            }

            if (usuario.SenhaHash != senha)
            {
                ViewBag.Error = "Senha inválida.";
                return View();
            }

            var usuarioJson = JsonConvert.SerializeObject(usuario);
            HttpContext.Session.SetString("UsuarioLogado", usuarioJson);

            if (lembrarDeMim)
            {
                var options = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = true
                };
                // Armazena apenas o ID do usuário no cookie
                Response.Cookies.Append("UsuarioId", usuario.IdUsuario!.ToString(), options);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult RecoverPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecoverPassword(string email)
        {
            // Verificar se o e-mail está cadastrado
            var usuarios = await _googleSheetsService.ObterUsuarios();
            var usuario = usuarios.FirstOrDefault(u => u.Email!.Trim().Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

            if (usuario == null)
            {
                ViewBag.Error = "E-mail não encontrado. Por favor, tente novamente ou realize um novo cadastro.";
                return View();
            }

            // Gerar código de verificação
            var codigoConfirmacao = new Random().Next(100000, 999999).ToString();

            // Salvar o código e o e-mail na sessão
            HttpContext.Session.SetString("CodigoConfirmacao", codigoConfirmacao);
            HttpContext.Session.SetString("EmailRecuperacao", email);
            HttpContext.Session.SetString("CodigoGeradoEm", DateTime.Now.ToString());

            // Enviar e-mail com o código
            await EnviarEmailConfirmacaoTrocarSenha(email, usuario.NomeUsuario!, codigoConfirmacao);

            // Redirecionar para a tela de verificação
            return RedirectToAction("TwoFactorResetPassword", "Auth");
        }

        [HttpGet]
        public IActionResult TwoFactorResetPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyTwoFactorResetPassword(string codigoConfirmacao)
        {
            var codigoSessao = HttpContext.Session.GetString("CodigoConfirmacao");

            if (codigoSessao == null || codigoConfirmacao != codigoSessao)
            {
                ViewBag.Error = "Código de confirmação inválido.";
                return View("TwoFactorResetPassword");
            }

            // Verificar o tempo de expiração (30 minutos)
            var tempoGeracao = HttpContext.Session.GetString("CodigoGeradoEm");
            if (tempoGeracao == null || DateTime.Now.Subtract(DateTime.Parse(tempoGeracao)).TotalMinutes > 30)
            {
                ViewBag.Error = "O código expirou. Por favor, solicite um novo.";
                return RedirectToAction("RecoverPassword", "Auth");
            }

            // Limpar o código da sessão para evitar reuso
            HttpContext.Session.Remove("CodigoConfirmacao");
            HttpContext.Session.Remove("CodigoGeradoEm");

            // Redirecionar para a tela de redefinição de senha
            return RedirectToAction("NewPassword", "Auth");
        }

        [HttpGet]
        public IActionResult NewPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewPassword(string novaSenha, string confirmaSenha)
        {
            if (novaSenha != confirmaSenha)
            {
                ViewBag.Error = "As senhas não coincidem.";
                return View();
            }

            var emailRecuperacao = HttpContext.Session.GetString("EmailRecuperacao");
            if (string.IsNullOrEmpty(emailRecuperacao))
            {
                ViewBag.Error = "Sessão expirada. Por favor, solicite uma nova redefinição de senha.";
                return RedirectToAction("RecoverPassword", "Auth");
            }

            // Atualizar a senha no Google Sheets
            await _googleSheetsService.AtualizarSenhaUsuario(emailRecuperacao, novaSenha);

            // Limpar dados da sessão
            HttpContext.Session.Remove("EmailRecuperacao");

            TempData["Success"] = "Senha redefinida com sucesso!";

            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nome, string email, string senha, IFormFile? fotoPerfil, bool lembrarDeMim)
        {
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Error = "Todos os campos obrigatórios devem ser preenchidos.";
                return View();
            }

            var usuarios = await _googleSheetsService.ObterUsuarios();
            if (usuarios.Any(u => u.Email == email))
            {
                ViewBag.Error = "E-mail já cadastrado.";
                return View();
            }

            string fotoBase64 = fotoPerfil != null && fotoPerfil.Length > 0
                ? await ConvertToBase64(fotoPerfil)
                : GetDefaultUserImageBase64();

            // Gerar código de confirmação
            var codigoConfirmacao = new Random().Next(100000, 999999).ToString();

            // Salvar os dados temporariamente na sessão
            HttpContext.Session.SetString("TempUser", JsonConvert.SerializeObject(new
            {
                NomeUsuario = nome,
                Email = email,
                SenhaHash = senha,
                FotoPerfil = fotoBase64
            }));

            // Armazenar a opção "Lembrar de mim"
            HttpContext.Session.SetString("LembrarDeMim", lembrarDeMim.ToString());

            HttpContext.Session.SetString("CodigoConfirmacao", codigoConfirmacao);

            // Armazenar o tempo de criação do código
            HttpContext.Session.SetString("CodigoGeradoEm", DateTime.Now.ToString());

            // Enviar o e-mail com o código
            await EnviarEmailConfirmacaoRegister(email, nome, codigoConfirmacao);

            // Redirecionar para a tela de confirmação
            return RedirectToAction("TwoFactorRegister", "Auth");
        }

        private static async Task EnviarEmailConfirmacaoRegister(string email, string nome, string codigo)
        {
            using var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new System.Net.NetworkCredential("contato.alportech@gmail.com", "fefp rhun wlkf aijq"),
                EnableSsl = true
            };

            var mensagem = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress("contato.alportech@gmail.com", "Dashboard Ambiental"),
                Subject = "Ativar sua conta Dashboard Ambiental",
                Body = $@"
            Olá {nome},<br><br>
            Obrigado por se registrar no Dashboard Ambiental.<br><br>
            Utilize o seguinte código para completar o registro da sua conta:<br><br>
            <strong>{codigo}</strong><br><br>
            Este código irá expirar em 30 minutos. Se você não se inscreveu para uma conta Dashboard Ambiental, você pode ignorar este e-mail com segurança.<br><br>
            Atenciosamente,<br><br>
            Equipe Alportech!",
                IsBodyHtml = true
            };

            mensagem.To.Add(email);
            await smtp.SendMailAsync(mensagem);
        }

        private static async Task EnviarEmailConfirmacaoTrocarSenha(string email, string nome, string codigo)
        {
            using var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new System.Net.NetworkCredential("contato.alportech@gmail.com", "fefp rhun wlkf aijq"),
                EnableSsl = true
            };

            var mensagem = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress("contato.alportech@gmail.com", "Dashboard Ambiental"),
                Subject = "Seu código de redefinição de senha do Dashboard Ambiental",
                Body = $@"
                Olá {nome},<br><br>
                Nós recebemos uma solicitação para redefinir a sua senha do Dashboard Ambiental.<br><br>
                Utilize o seguinte código para redefinir sua senha:<br><br>
                <strong>{codigo}</strong><br><br>
                Este código irá expirar em 30 minutos. Se você não solicitou a redefinição de sua senha, nenhuma ação é necessária — por gentileza, ignore este e-mail.<br><br>
                Atenciosamente,<br><br>
                Equipe Alportech!",
                IsBodyHtml = true
            };

            mensagem.To.Add(email);
            await smtp.SendMailAsync(mensagem);
        }

        [HttpGet]
        public IActionResult TwoFactorRegister() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactorRegister(string codigoConfirmacao)
        {
            var codigoSessao = HttpContext.Session.GetString("CodigoConfirmacao");
            if (codigoSessao == null || codigoConfirmacao != codigoSessao)
            {
                ViewBag.Error = "Código de confirmação inválido.";
                return View("TwoFactorRegister");
            }

            // Verificar o tempo de expiração (30 minutos)
            var tempoGeracao = HttpContext.Session.GetString("CodigoGeradoEm");
            if (tempoGeracao == null)
            {
                ViewBag.Error = "O código expirou. Por favor, solicite um novo código.";
                return View("TwoFactor");
            }

            var tempoGerado = DateTime.Parse(tempoGeracao);
            if (DateTime.Now.Subtract(tempoGerado).TotalMinutes > 30)
            {
                ViewBag.Error = "O código expirou. Por favor, solicite um novo código.";
                return View("TwoFactor");
            }

            var tempUserJson = HttpContext.Session.GetString("TempUser");
            if (tempUserJson == null)
            {
                ViewBag.Error = "Sessão expirada. Por favor, registre-se novamente.";
                return RedirectToAction("Register", "Auth");
            }

            var tempUserDeserialize = JsonConvert.DeserializeObject<dynamic>(tempUserJson);

            // Salvar o usuário no Google Sheets
            await _googleSheetsService.CriarUsuario(
                tempUserDeserialize!.NomeUsuario.ToString(),
                tempUserDeserialize.Email.ToString(),
                tempUserDeserialize.SenhaHash.ToString(),
                tempUserDeserialize.FotoPerfil.ToString()
            );

            var usuarios = await _googleSheetsService.ObterUsuarios();
            var usuario = usuarios.FirstOrDefault(u => u.Email!.ToString() == tempUserDeserialize.Email.ToString());
            var tempUserSerialize = JsonConvert.SerializeObject(usuario);
            HttpContext.Session.SetString("UsuarioLogado", tempUserSerialize);

            // Verificar se "Lembrar de mim" foi selecionado e salvar o cookie com o ID do usuário
            if (HttpContext.Session.GetString("LembrarDeMim")!.ToLower() == "true")
            {
                var usuarioId = usuario!.IdUsuario;
                var options = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = true
                };
                Response.Cookies.Append("UsuarioId", usuarioId!, options);
            }

            // Limpar dados da sessão
            HttpContext.Session.Remove("TempUser");
            HttpContext.Session.Remove("CodigoConfirmacao");
            HttpContext.Session.Remove("CodigoGeradoEm");

            return RedirectToAction("Index", "Dashboard");
        }

        private static async Task<string> ConvertToBase64(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();
            return Convert.ToBase64String(fileBytes);
        }

        private static string GetDefaultUserImageBase64()
        {
            // Caminho para a imagem padrão (exemplo de ícone de usuário)
            string defaultImagePath = "wwwroot/images/default_user_icon.png";
            byte[] imageBytes = System.IO.File.ReadAllBytes(defaultImagePath);
            return Convert.ToBase64String(imageBytes);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UsuarioId");
            return RedirectToAction("Login", "Auth", new { cacheBuster = DateTime.Now.Ticks });
        }
    }
}