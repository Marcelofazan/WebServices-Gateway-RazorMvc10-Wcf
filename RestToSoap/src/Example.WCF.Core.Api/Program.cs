using Example.WCF.Core.Application;
using Example.WCF.Core.Domain;
using Example.WCF.Core.Infrastructure;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// 1. Configurações de API e Documentação OpenAPI (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Força o caminho absoluto direto na pasta padrão do ASP.NET local
string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string certPath = Path.Combine(userProfileFolder, ".aspnet", "https", "soapapp.pfx");
string certPassword = "CrypticPassword99!";
string certThumbprint = "FFD7FA6D736A9EA199B45E045D25CC30492EE6A5";

X509Certificate2? gatewayCertificate = null;
if (File.Exists(certPath))
{
    gatewayCertificate = new X509Certificate2(
        certPath,
        certPassword,
        X509KeyStorageFlags.MachineKeySet |
        X509KeyStorageFlags.Exportable |
        X509KeyStorageFlags.PersistKeySet
    );
}
// 2. CORREÇÃO CRÍTICA DO FALLBACK: Busca e extrai direto do Repositório pelo Thumbprint
else if (!string.IsNullOrEmpty(certThumbprint))
{
    // Usamos OpenFlags.ReadWrite para garantir permissão de exportação da chave privada no Windows Store
    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadOnly);

    var colecao = store.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, validOnly: false);

    if (colecao.Count > 0)
    {
        // Pega o certificado diretamente da coleção do Windows
        var certDoRepositorio = colecao[0];

        // CORREÇÃO: Exporta e reinsbancia com as flags de persistência/exportação de chave privada
        // Isso resolve o erro 'Unable to load private key from PFX' quando lido do Windows Store
        var bytesRaw = certDoRepositorio.Export(X509ContentType.Pkcs12, certPassword);

        gatewayCertificate = new X509Certificate2(
            bytesRaw,
            certPassword,
            X509KeyStorageFlags.MachineKeySet |
            X509KeyStorageFlags.Exportable |
            X509KeyStorageFlags.PersistKeySet
        );
    }
}
if (gatewayCertificate == null)
{
    throw new CryptographicException($"Falha Crítica: Certificado não encontrado no arquivo '{certPath}' e nem pelo Thumbprint '{certThumbprint}'.");
}

builder.Services.AddSingleton(gatewayCertificate);

// 5. Registro das injeções de dependência das bibliotecas do sistema
builder.Services.AddApplication();
builder.Services.AddDomain();
builder.Services.AddInfrastructure();

var app = builder.Build();

// 6. Pipeline de execução HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Importante: MapControllers deve ficar preferencialmente após as definições de segurança do pipeline
app.MapControllers();

app.Run();