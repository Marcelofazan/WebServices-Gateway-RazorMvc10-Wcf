using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// CORREÇÃO DE AMBIGUIDADE: Aliases para isolar a camada de API da camada de Domínio
using DomainSoapRequest = Example.WCF.Core.Domain.Models.SoapRequest;
using DomainSecureXHeader = Example.WCF.Core.Domain.Models.SecureXHeader;
using Example.WCF.Core.Infrastructure.Services;

namespace Example.WCF.Core.Api.Controllers
{
    [ApiController]
    [Route("api/v1/consumer-submit")]
    public class ConsumerSubmitController(
        SoapMessageService soapMessageService,
        IHttpClientFactory httpClientFactory) : ControllerBase
    {
        private static readonly string TryAspUrl = "https://api21mobile51789.tryasp.net/api/pessoa";
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = false };

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] ApiSoapRequest request)
        {
            if (request == null || request.Header == null)
            {
                return BadRequest("Payload de requisição SOAP inválido no pipeline do Gateway.");
            }

            // =================================================================
            // EXTRAÇÃO ROBUSTA COM NEWTONSOFT (FIX DE CAPTURA DE ID)
            // =================================================================
            int idDesejado = 0;
            if (!string.IsNullOrEmpty(request.Body))
            {
                try
                {
                    // Decodifica o Base64 vindo do Body do Frontend
                    string jsonFiltro = Encoding.UTF8.GetString(Convert.FromBase64String(request.Body));

                    // Usa o Newtonsoft dinâmico para varrer o JSON sem se importar com maiúsculas/minúsculas
                    dynamic objetoFiltro = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonFiltro);

                    if (objetoFiltro != null)
                    {
                        if (objetoFiltro.idPessoa != null) idDesejado = (int)objetoFiltro.idPessoa;
                        else if (objetoFiltro.IdPessoa != null) idDesejado = (int)objetoFiltro.IdPessoa;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gateway] Falha ao extrair ID: {ex.Message}");
                }
            }

            try
            {
                // =================================================================
                // STEP 1: LEITURA EXTRAINDO O CONTEÚDO DE DENTRO DE "DATA" (FIX)
                // =================================================================
                var httpClient = httpClientFactory.CreateClient();
                List<PessoaModel> dadosLegados = new();

                try
                {
                    var respostaTryAsp = await httpClient.GetAsync(TryAspUrl);

                    if (respostaTryAsp.IsSuccessStatusCode)
                    {
                        string jsonBrutoDoServidor = await respostaTryAsp.Content.ReadAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(jsonBrutoDoServidor))
                        {
                            string jsonTratado = jsonBrutoDoServidor.Trim();

                            // Como a raiz agora começa com '{', tratamos como o objeto de resposta da API
                            if (jsonTratado.StartsWith("{"))
                            {
                                // Desserializa o wrapper completo { data: [...], messages: "", status: true }
                                var wrapper = JsonSerializer.Deserialize<TryAspResponseWrapper>(jsonTratado, JsonOptions);

                                if (wrapper != null && wrapper.Data != null)
                                {
                                    dadosLegados = wrapper.Data; // Coleta a lista real de pessoas que estava escondida!
                                }
                            }
                        }
                    }
                    else
                    {
                        return StatusCode((int)respostaTryAsp.StatusCode, $"A API remota recusou a listagem. Status: {(int)respostaTryAsp.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(502, $"Falha do Gateway ao tentar ler a API remota do legado (tryasp): {ex.Message}");
                }

                // =================================================================
                // STEP 2 e 3: POPULAÇÃO DOS FIELDS E TRATAMENTO NEwTONSOFT
                // =================================================================
                var listaChaveValor = new List<KeyValuePair<string, string>>();

                // Convertemos os dados reais trazidos para o array exigido pela assinatura do CreateSpxBody
                foreach (var pessoa in dadosLegados)
                {
                    listaChaveValor.Add(new KeyValuePair<string, string>($"Pessoa_{pessoa.IdPessoa}_Id", pessoa.IdPessoa.ToString()));
                    listaChaveValor.Add(new KeyValuePair<string, string>($"Pessoa_{pessoa.IdPessoa}_RazaoSocial", pessoa.RazaoSocial ?? ""));
                    listaChaveValor.Add(new KeyValuePair<string, string>($"Pessoa_{pessoa.IdPessoa}_CnpjCpf", pessoa.CnpjCpf ?? ""));
                    listaChaveValor.Add(new KeyValuePair<string, string>($"Pessoa_{pessoa.IdPessoa}_Email", pessoa.Email ?? ""));
                    listaChaveValor.Add(new KeyValuePair<string, string>($"Pessoa_{pessoa.IdPessoa}_Telefone", pessoa.Telefone ?? ""));
                }

                var objetoWcfConcreto = new
                {
                    Fields = listaChaveValor.ToArray()
                };

                // Serializa mantendo em linha única para o leitor de buffer do Newtonsoft não quebrar
                string assemblyConcreto = "Example.WCF.Core.Domain.Models.VXConsumerSubmitMessage, Example.WCF.Core.Domain";
                string fieldsJsonCompacto = JsonSerializer.Serialize(objetoWcfConcreto.Fields);

                string jsonPayloadFinal = "{" +
                    $"\"$type\":\"{assemblyConcreto}\"," +
                    $"\"Fields\":{fieldsJsonCompacto}" +
                "}";

                request.Body = jsonPayloadFinal;

                // =================================================================
                // RESOLUÇÃO DA AMBIGUIDADE E CONVERSÃO COMPLETA DE TIPOS DO DOMÍNIO
                // =================================================================
                var domainRequest = new DomainSoapRequest
                {
                    Body = request.Body,
                    Header = new DomainSecureXHeader
                    {
                        ConsumerBusinessUnit = Guid.TryParse(request.Header.ConsumerBusinessUnit, out Guid cbu) ? cbu : null,
                        ProviderBusinessUnit = Guid.TryParse(request.Header.ProviderBusinessUnit, out Guid pbu) ? pbu : null,
                        ConsumerReference = request.Header.ConsumerReference,
                        ProviderReference = request.Header.ProviderReference,
                        InitiatingIP = request.Header.InitiatingIP,
                        ExchangeReference = long.TryParse(request.Header.ExchangeReference, out long er) ? er : null,
                        ProductId = byte.TryParse(request.Header.ProductId, out byte pid) ? pid : (byte)0,
                        TransactionStatus = byte.TryParse(request.Header.TransactionStatus, out byte ts) ? ts : (byte)0
                    }
                };

                // =================================================================
                // STEP 4: FLUXO ORIGINAL DE ASSINATURA DIGITAL (WS-SECURITY)
                // =================================================================
                // O pipeline gerará o XML com sucesso usando o payload do GetAsync
                string xmlSoapAssinado = soapMessageService.GenerateSoapMessage(domainRequest);

                // =================================================================
                // STEP 5: MONTAGEM E RETORNO DO ENVELOPE SEGURO PARA O FRONTEND
                // =================================================================
                // Envelopamos a lista populada do GetAsync para a View ler direto sem expor rotas REST
                string jsonRegistrosReal = JsonSerializer.Serialize(dadosLegados);
                string base64DadosReal = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonRegistrosReal));

                string xmlRespostaMimetizada =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<soap:Envelope xmlns:soap=\"http://w3.org\">" +
                        "<soap:Body>" +
                            $"<ObterTodasPessoasResult>{base64DadosReal}</ObterTodasPessoasResult>" +
                        "</soap:Body>" +
                    "</soap:Envelope>";

                return Content(xmlRespostaMimetizada, "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Falha crítica interna no processamento criptográfico do Gateway: {ex.Message} | Rastro: {ex.StackTrace}");
            }
        }
    }

    // =================================================================
    // CONTRATOS E SCHEMAS EXCLUSIVOS DA CAMADA DE API 
    // =================================================================
    public class PessoaModel
    {
        public int IdPessoa { get; set; }
        public string RazaoSocial { get; set; } = string.Empty;
        public string CnpjCpf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class ApiSoapRequest
    {
        public ApiSecureXHeader Header { get; set; } = new();
        public string Body { get; set; } = string.Empty;
    }

    public class ApiSecureXHeader
    {
        public string ConsumerBusinessUnit { get; set; } = string.Empty;
        public string ConsumerReference { get; set; } = string.Empty;
        public string ExchangeReference { get; set; } = string.Empty;
        public string InitiatingIP { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string ProviderBusinessUnit { get; set; } = string.Empty;
        public string ProviderReference { get; set; } = string.Empty;
    }

    public class TryAspResponseWrapper
    {
        public List<PessoaModel> Data { get; set; } = new();
        public string Messages { get; set; } = string.Empty;
        public bool Status { get; set; }
    }
}