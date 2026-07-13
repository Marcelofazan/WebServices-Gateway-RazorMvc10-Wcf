using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Example.WCF.Core.Domain.Models;
using Example.WCF.Core.Domain.Services;

namespace Example.WCF.Core.Infrastructure.Services;

public class CertificateService
{
    private readonly X509Certificate2? _certificate;
    private readonly AppSettings _appSettings;

    public CertificateService(AppSettingsService appSettingsService)
    {
        _appSettings = appSettingsService.GetAppSettings();
        _certificate = LoadCertificate();
    }

    private X509Certificate2? LoadCertificate()
    {
        string? thumbprint = _appSettings.CertThumbprint?.Replace(" ", "").ToUpperInvariant();

        if (OperatingSystem.IsWindows() && !string.IsNullOrEmpty(thumbprint))
        {
            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            X509Certificate2? certOriginal = store.Certificates
                .OfType<X509Certificate2>()
                .FirstOrDefault(c => c.Thumbprint?.ToUpperInvariant() == thumbprint);

            store.Close();

            if (certOriginal != null)
            {
                // CORREÇÃO CRÍTICA: Se o certificado foi achado na Store do Windows,
                // nós NÃO tentamos exportá-lo (o que gerava o erro silencioso).
                // Retornamos ele diretamente. Se ele falhar na assinatura, significa
                // que o arquivo físico (GetCertFromFileLocation) é o caminho correto para o ambiente de testes.
                return certOriginal;
            }
        }

        return GetCertFromFileLocation();
    }

    private X509Certificate2? GetCertFromFileLocation()
    {
        // 1. Tenta carregar usando o caminho do appsettings corporativo
        if (!string.IsNullOrEmpty(_appSettings.CertStorePath) && File.Exists(_appSettings.CertStorePath))
        {
            return new X509Certificate2(
                _appSettings.CertStorePath,
                _appSettings.CertStorePassword ?? "CrypticPassword99!",
                X509KeyStorageFlags.EphemeralKeySet
            );
        }

        // 2. FALLBACK BLINDADO PARA AMBIENTE DE TESTE:
        // Se o appsettings do projeto de teste veio vazio, busca o PFX direto no perfil do usuário local
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string fallbackPath = Path.Combine(userProfile, ".aspnet", "https", "soapapp.pfx");
        string fallbackPassword = "CrypticPassword99!";

        if (File.Exists(fallbackPath))
        {
            return new X509Certificate2(
                fallbackPath,
                fallbackPassword,
                X509KeyStorageFlags.EphemeralKeySet
            );
        }

        // Se chegar aqui, o arquivo realmente não existe fisicamente em lugar nenhum
        return null;
    }

    public string GetBinarySecurityToken() =>
        _certificate != null ? Convert.ToBase64String(_certificate.RawData) : string.Empty;

    public string GetSubjectKeyIdentifier()
    {
        if (_certificate == null) return string.Empty;

        byte[] publicKeyBytes = _certificate.GetPublicKey();
        byte[] skiBytes = SHA1.HashData(publicKeyBytes);
        return Convert.ToBase64String(skiBytes);
    }

    public RSA? GetPrivateKey() => _certificate?.GetRSAPrivateKey();

    public RSA? GetPublicKey() => _certificate?.GetRSAPublicKey();
}