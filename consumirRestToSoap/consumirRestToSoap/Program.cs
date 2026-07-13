var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("GatewayClient", client =>
{
    // Lê o endereço do Gateway direto da nova seção do seu appsettings.json
    string baseUrl = builder.Configuration["GatewaySettings:BaseUrl"]
                     ?? "https://localhost:7150/"; // Fallback de segurança caso a chave suma

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);

    // Configura os cabeçalhos padrão de aceitação de conteúdo limpo para JSON
    client.DefaultRequestHeaders.Clear(); // Remove lixo pré-existente
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pessoa}/{action=ListAll}/{id?}");

app.Run();
