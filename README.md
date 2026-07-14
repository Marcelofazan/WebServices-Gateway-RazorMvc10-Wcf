
## 🌐 WS-Gateway-RazorMvc10-Wcf
Exemplo Auditoria de integração API para SOAP x.509 com WS-Security em C# ASP.NET CORE 10  Razor Mvc e .NET Core 8. 

**Segurança de Ponta a Ponta**
- Para proteger o tráfego de dados confidenciais, ela implementa segurança criptográfica robusta baseada em padrões de mercado (WS-Security). 
- Ela carrega um certificado digital assimétrico real (.pfx) para abrir chaves criptografadas via RSA (PKCS1) e, a partir daí, descriptografa o corpo das mensagens utilizando o algoritmo simétrico AES-256 (CBC/PKCS7).
  
#### 🎨 Aqui está uma demonstração do projeto
<img width="700" height="350" alt="MonitorSoap" src="https://github.com/user-attachments/assets/3b4909b4-c551-452a-a8f6-ec75409506ea" />

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

#### 📁 RestToSoap
#### 🔄 Executar a aplicação
- Para inciar a aplicação 
```bash
dotnet run
```

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

#### 📁 consumirRestToSoap
A Aplicação gera duas requisições GetAll e Get. 

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
- O Projeto [A] que é uma API com banco de dados MySQL hospedada, é compartilhada com o Projeto [B].
- O Projeto [B] consome a API e criptografa, transformando sua saida em SOAP WS-Security.
- O Projeto [C] se conecta com interface ao Projeto [B] consumido dados criptografados. 

#### 📁 Api8-Mobile-Mysql
API que é compartilhada o EndPoint para o projeto **RestToSoap**

#### ⚙️ Configuração Certificado x.509 
- Abrir o Editor PowerShell 
Pressione Windows + R no teclado e Digite **powershell_ise** e pressione Enter.
- Execute o bloco inteiro, o codigo abaixo gera o certificado que usa dados **Criptografados** 
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

- Para gerar e instalar o Certificado na pasta de Autoridades de Certificação Raiz Confiáveis do seu computador local. Execute o bloco inteiro
```bash 
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=localhost*" } | Select-Object -First 1
$rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
$rootStore.Open("ReadWrite")
$rootStore.Add($cert)
$rootStore.Close()
```
- Execute este comando para visualizar o Thumbprint do Certificado criado
```bash 
Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=localhost*" } | Select-Object Subject, Thumbprint, NotAfter
```


