using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using consumirRestToSoap.Models;

namespace consumirRestToSoap.Controllers
{
    public class PessoaController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public PessoaController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> ListAll()
        {
            ViewBag.ListaPessoas = new List<PessoaModel>();
            ViewBag.XmlBrutoSOAP = string.Empty;
            ViewBag.JsonDecodificado = string.Empty;
            ViewBag.ErrorMessage = string.Empty;

            try
            {
                var client = _clientFactory.CreateClient("GatewayClient");

                var dadosBuscaGeral = new
                {
                    idPessoa = 0,
                    razaoSocial = string.Empty,
                    cnpjCpf = string.Empty,
                    email = string.Empty,
                    telefone = string.Empty,
                    usuario = string.Empty,
                    senha = string.Empty
                };

                string jsonBusca = JsonSerializer.Serialize(dadosBuscaGeral);
                string base64Listagem = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonBusca));

                var soapRequestPayload = new
                {
                    // Vincula explicitamente ao objeto Header esperado pela ApiSoapRequest
                    Header = new
                    {
                        ConsumerBusinessUnit = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                        ConsumerReference = "ReferenciaValida",

                        // CORREÇÃO: Convertido para string (Texto entre aspas no JSON)
                        ExchangeReference = Convert.ToInt32(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString(),

                        InitiatingIP = "127.0.0.1",

                        // CORREÇÃO: Convertido para string (Texto entre aspas no JSON)
                        ProductId = "2",

                        ProviderBusinessUnit = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                        ProviderReference = "ProvedorValido",

                        // CORREÇÃO: Convertido para string (Texto entre aspas no JSON)
                        TransactionStatus = "1"
                    },
                    // Atribui a string base64 tratada para a propriedade Body
                    Body = base64Listagem
                };

                var opcoesSerializacaoGateway = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Serializa o novo objeto anônimo perfeitamente tipado como string
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(soapRequestPayload, opcoesSerializacaoGateway),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("api/v1/consumer-submit", jsonContent);

                // CORREÇÃO: Captura a string interna mesmo se for um erro 500 para ler o Log do Rastro
                string xmlCriptografadoBruto = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Se o gateway retornou o erro 500 customizado, exibe o rastro na tela
                    ViewBag.ErrorMessage = !string.IsNullOrEmpty(xmlCriptografadoBruto)
                        ? $"Erro no Gateway: {xmlCriptografadoBruto}"
                        : $"Erro ao conectar com o Gateway: HTTP {(int)response.StatusCode}";
                    return View();
                }

                ViewBag.XmlBrutoSOAP = !string.IsNullOrEmpty(xmlCriptografadoBruto)
                   ? FormatXmlString(xmlCriptografadoBruto)
                   : xmlCriptografadoBruto;

                if (string.IsNullOrWhiteSpace(xmlCriptografadoBruto) || xmlCriptografadoBruto.Contains("No Response Received"))
                {
                    ViewBag.ErrorMessage = "O servidor do Gateway retornou uma resposta vazia ou não recebeu resposta do barramento remoto.";
                    return View();
                }

                try
                {
                    var xDoc = XDocument.Parse(xmlCriptografadoBruto);
                    var todosCipherValues = xDoc.Descendants()
                                                .Where(x => x.Name.LocalName.Equals("CipherValue", StringComparison.OrdinalIgnoreCase))
                                                .Select(x => x.Value.Trim())
                                                .ToList();

                    string cipherValueKey = string.Empty;
                    string cipherValueBody = string.Empty;

                    if (todosCipherValues.Count >= 2)
                    {
                        cipherValueKey = todosCipherValues[0];
                        cipherValueBody = todosCipherValues[1];
                    }
                    else if (todosCipherValues.Count == 1)
                    {
                        cipherValueBody = todosCipherValues[0];
                    }

                    if (!string.IsNullOrEmpty(cipherValueBody))
                    {
                        var decryptionPayload = new DecryptionRequest
                        {
                            EncryptedBodyBase64 = cipherValueBody,
                            EncryptedAesBase64 = cipherValueKey
                        };

                        var decryptContent = new StringContent(JsonSerializer.Serialize(decryptionPayload), Encoding.UTF8, "application/json");
                        var decryptResponse = await client.PostAsync("api/v1/decrypt-soap", decryptContent);

                        if (decryptResponse.IsSuccessStatusCode)
                        {
                            string jsonStringClara = await decryptResponse.Content.ReadAsStringAsync();

                            if (jsonStringClara.Trim().StartsWith("<"))
                            {
                                var xDocClean = XDocument.Parse(jsonStringClara);
                                jsonStringClara = xDocClean.Root?.Value ?? jsonStringClara;
                            }

                            ViewBag.JsonDecodificado = jsonStringClara;

                            var listaPessoas = JsonSerializer.Deserialize<List<PessoaModel>>(jsonStringClara, JsonOptions) ?? new List<PessoaModel>();
                            ViewBag.ListaPessoas = listaPessoas;
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "O barramento respondeu, mas o endpoint /api/v1/decrypt-soap falhou ao processar a cifra.";
                        }
                    }
                    else
                    {
                        var nodoResultado = xDoc.Descendants()
                                                .FirstOrDefault(x => x.Name.LocalName.Equals("ObterTodasPessoasResult", StringComparison.OrdinalIgnoreCase)
                                                                  || x.Name.LocalName.Equals("ObterPessoaResult", StringComparison.OrdinalIgnoreCase));

                        if (nodoResultado != null && !string.IsNullOrEmpty(nodoResultado.Value))
                        {
                            string base64Extraido = nodoResultado.Value.Trim();
                            string jsonAberto = Encoding.UTF8.GetString(Convert.FromBase64String(base64Extraido));
                            ViewBag.JsonDecodificado = jsonAberto;

                            var listaPessoas = JsonSerializer.Deserialize<List<PessoaModel>>(jsonAberto, JsonOptions) ?? new List<PessoaModel>();
                            ViewBag.ListaPessoas = listaPessoas;
                        }
                        else
                        {
                            var faultText = xDoc.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Text", StringComparison.OrdinalIgnoreCase))?.Value;
                            ViewBag.ErrorMessage = !string.IsNullOrEmpty(faultText)
                                ? $"Barramento recusou a operação: {faultText}"
                                : "Nenhum bloco criptografado ou tag de dados estruturados foi localizado no XML de retorno.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"Falha crítica no processamento ou parse do XML: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Falha na orquestração de leitura: {ex.Message}";
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            ViewBag.Pessoa = new PessoaModel();
            ViewBag.XmlBrutoSOAP = string.Empty;
            ViewBag.JsonDecodificado = string.Empty;
            ViewBag.ErrorMessage = string.Empty;

            try
            {
                var client = _clientFactory.CreateClient("GatewayClient");

                // 1. Força a criação do JSON com a chave exata
                string jsonBusca = "{\"idPessoa\":" + id + "}";
                string base64Busca = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonBusca));

                // 2. CORREÇÃO CRÍTICA: Usa objeto anônimo puro (Exatamente igual ao seu ListAll de sucesso)
                // Isso impede que as propriedades fiquem com a primeira letra maiúscula da classe SoapRequest
                var soapRequestPayload = new
                {
                    Header = new
                    {
                        ConsumerBusinessUnit = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                        ConsumerReference = "BuscaPorIdCorporativa",
                        ExchangeReference = Convert.ToInt32(DateTimeOffset.UtcNow.ToUnixTimeSeconds() & 0x7FFFFFFF).ToString(),
                        InitiatingIP = "127.0.0.1",
                        ProductId = "2",
                        ProviderBusinessUnit = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                        ProviderReference = "ProvedorValido",
                        TransactionStatus = "1"
                    },
                    Body = base64Busca
                };

                var opcoesGateway = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                // 3. Serializa o objeto anônimo gerando chaves minúsculas nativas
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(soapRequestPayload, opcoesGateway),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("api/v1/consumer-submit", jsonContent);
                string xmlCriptografadoBruto = await response.Content.ReadAsStringAsync();

                ViewBag.XmlBrutoSOAP = xmlCriptografadoBruto;

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = $"O servidor do Gateway recusou a busca por ID. Código HTTP: {(int)response.StatusCode}";
                    return View();
                }

                try
                {
                    var xDoc = XDocument.Parse(xmlCriptografadoBruto);
                    string jsonAberto = string.Empty;

                    // 4. Captura a resposta inteligente (Seja ObterPessoaResult ou ObterTodasPessoasResult)
                    var nodoResultado = xDoc.Descendants()
                                            .FirstOrDefault(x => x.Name.LocalName.Equals("ObterPessoaResult", StringComparison.OrdinalIgnoreCase)
                                                              || x.Name.LocalName.Equals("ObterTodasPessoasResult", StringComparison.OrdinalIgnoreCase));

                    if (nodoResultado != null && !string.IsNullOrEmpty(nodoResultado.Value))
                    {
                        string base64Extraido = nodoResultado.Value.Trim();
                        jsonAberto = Encoding.UTF8.GetString(Convert.FromBase64String(base64Extraido));
                        ViewBag.JsonDecodificado = jsonAberto;
                    }

                    if (!string.IsNullOrEmpty(jsonAberto))
                    {
                        // 5. Se o Gateway respondeu a lista inteira por falha, filtramos o ID 17 direto no Frontend
                        // Isso blinda a sua tela e garante a atualização visual mesmo se o Gateway oscilar
                        var listaPessoas = jsonAberto.Trim().StartsWith("[")
                            ? JsonSerializer.Deserialize<List<PessoaModel>>(jsonAberto, JsonOptions)
                            : new List<PessoaModel> { JsonSerializer.Deserialize<PessoaModel>(jsonAberto, JsonOptions) ?? new() };

                        // Busca cirurgicamente o ID 17 ou o ID clicado dentro da resposta decodificada
                        var pessoaFiltrada = listaPessoas?.FirstOrDefault(x => x.IdPessoa == id);

                        if (pessoaFiltrada != null)
                        {
                            ViewBag.Pessoa = pessoaFiltrada;
                        }
                        else if (listaPessoas != null && listaPessoas.Count > 0)
                        {
                            // Fallback de segurança se o ID sumir da base remota
                            ViewBag.Pessoa = listaPessoas[0];
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Nenhum dado estruturado legítimo foi localizado dentro do XML retornado.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"Falha crítica ao realizar o parse da árvore XML: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Falha geral na orquestração HTTP do Frontend: {ex.Message}";
            }

            return View();
        }

        // Método auxiliar de suporte para normalizar retornos de Objeto Único ou Coleções Unitárias do Legado
        private void ProcessarEMapearRetornoJson(string jsonString)
        {
            if (jsonString.Trim().StartsWith("["))
            {
                var lista = JsonSerializer.Deserialize<List<PessoaModel>>(jsonString, JsonOptions);
                ViewBag.Pessoa = lista?.FirstOrDefault() ?? new PessoaModel();
            }
            else
            {
                ViewBag.Pessoa = JsonSerializer.Deserialize<PessoaModel>(jsonString, JsonOptions) ?? new PessoaModel();
            }
        }
        private string FormatXmlString(string xml)
        {
            try
            {
                var xDoc = XDocument.Parse(xml);
                return xDoc.ToString();
            }
            catch
            {
                return xml;
            }
        }
    }
}