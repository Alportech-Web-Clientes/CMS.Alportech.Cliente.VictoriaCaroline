using CMS.Alportech.Cliente.VictoriaCaroline.Services;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<GoogleSheetsService>(client =>
{
    client.BaseAddress = new Uri("https://docs.google.com/spreadsheets/d/1ih_vDXBSDPJgpBFScJbGrfZ-IBWuXo4uyNBBvVjl8QY/gviz/tq?tqx=out:csv");
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.Use(async (context, next) =>
{
    var usuarioLogado = context.Session.GetString("UsuarioLogado");
    var usuarioId = context.Request.Cookies["UsuarioId"];

    if (string.IsNullOrEmpty(usuarioLogado) && !string.IsNullOrEmpty(usuarioId))
    {
        // Busca o usuário pelo ID e armazena na sessão
        var googleSheetsService = context.RequestServices.GetRequiredService<GoogleSheetsService>();
        var usuarios = await googleSheetsService.ObterUsuarios();
        var usuario = usuarios.FirstOrDefault(u => u.IdUsuario!.ToString() == usuarioId);
        if (usuario != null)
        {
            var usuarioJson = JsonConvert.SerializeObject(usuario);
            context.Session.SetString("UsuarioLogado", usuarioJson);
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();