
## 🌐 WebServices-Gateway-RazorMvc10-Wcf
Exemplo de auditoria de integração API para SOAP com WS-Security em C# RazorMvc10 e ASP.NET Core 8. 

#### 🎨 Aqui está uma demonstração do projeto
<img width="700" height="350" alt="MonitorSoap" src="https://github.com/user-attachments/assets/3b4909b4-c551-452a-a8f6-ec75409506ea" />

- Segurança de Ponta a Ponta
- Para proteger o tráfego de dados confidenciais, ela implementa segurança criptográfica robusta baseada em padrões de mercado (WS-Security). 
- Ela carrega um certificado digital assimétrico real (.pfx) para abrir chaves criptografadas via RSA (PKCS1) e, a partir daí, descriptografa o corpo das mensagens utilizando o algoritmo simétrico AES-256 (CBC/PKCS7).


#### 📋 O que voçê vai ver nesse Projeto
| Tecnologia | Descrição |
|-----------|-----------|
| **HTTPClient**  | Classe primária utilizada para enviar solicitações HTTP e receber respostas de recursos identificados por um URI. |
| **WCF Client**  | Objeto local que atua como um intermediário (proxy), permitindo que seu aplicativo se comunique e consuma serviços remotos |
| **WS-Security**  | Camada de segurança para criptografia e autenticação de requisições. |

#### 💬 Requisitos do Projeto
- Certificado válido X.509
- Necessário acomplamento de serviços, o Frontend Auditoria depende do Gateway Rest Soap.

#### ⚠️ Alterar URL para a da API 
- Alterar em **ConsumerSubmitController.cs** o Endpoint da API Legada 
```bash
private static readonly string TryAspUrl = "https://[SUA_API_LEGADO].tryasp.net/api/pessoa";
```

#### 📁 Backend

- Modifique [SUA_API_LEGADO] , [SENHA_CERTIFICADO] e o [THUMBPRINT_CERTIFICADO] no arquivo **appsettings.json**, no trecho indicado: 

```bash 
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "CertStorePath": "%USERPROFILE%\\.aspnet\\https\\soapapp.pfx",
  "CertStorePassword": "[SENHA_CERTIFICADO]",
  "CertThumbprint": "[THUMBPRINT_CERTIFICADO]",
  "SoapEndpoint": "[SUA_API_LEGADO]",
  "SoapAction": "http://securex.common"
}
```

#### 📁 Frontend 
A aplicação consome a URL do projeto Rest-Soap que consome um GET e GETALL da API. 

#### 🔄 Executar a aplicação
- Para inciar a aplicação 
```bash
dotnet run
```

#### Fluxo da Estrutura Técnica   
```bash 
[Cliente / API REST] 
          |──> [Gateway REST-SOAP] 
                    |──(WS-Security / AES-256)
                              |──> [Serviço SOAP/WCF] 
                                        |──> [Frontend Razor MVC 10]
```

#### ⚙️ Configuração Certificado x.509 
- Executar Editor PowerShell 
Pressione Windows + R no teclado e Digite **powershell_ise** e pressione Enter.

```bash 
# 1. Garante a pasta de destino
mkdir -Force "$env:USERPROFILE\.aspnet\https"

# 2. Cria o certificado injetando a flag "-KeyExportPolicy Exportable" (O segredo do erro)
$cert = New-SelfSignedCertificate `
    -DnsName "localhost" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyUsage KeyEncipherment, DigitalSignature, DataEncipherment `
    -KeyExportPolicy Exportable `
    -Type Custom `
    -NotAfter (Get-Date).AddYears(2)

# 3. Define a senha padrão configurada na sua aplicação
$password = ConvertTo-SecureString -String "CrypticPassword99!" -Force -AsPlainText

# 4. Exporta o PFX garantindo que a chave privada vá junto com o arquivo físico
Export-PfxCertificate -Cert $cert -FilePath "$env:USERPROFILE\.aspnet\https\soapapp.pfx" -Password $password
```

- Certificado gerado está instalado na pasta de Autoridades de Certificação Raiz Confiáveis do seu computador local. 
- Execute este comando rápido no PowerShell como Administrador:

```bash 
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=localhost*" } | Select-Object -First 1
$rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
$rootStore.Open("ReadWrite")
$rootStore.Add($cert)
$rootStore.Close()
```

- Abra o PowerShell e execute este comando para visualizar o Thumbprint do certificado criado para o localhost:
powershell

```bash 
Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=localhost*" } | Select-Object Subject, Thumbprint, NotAfter
```

- Caso precise de erros de Certificado, remover qualquer certificado localhost antigo para não dar conflito de chaves
```bash 
Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=localhost*" } | Remove-Item
```


