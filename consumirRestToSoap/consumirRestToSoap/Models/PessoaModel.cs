
using System.ComponentModel.DataAnnotations;

namespace consumirRestToSoap.Models
{

    /// <summary>
    /// Representa a estrutura de dados real do Consumidor (Pessoa/Empresa).
    /// Utilizada tanto para validação do formulário de cadastro quanto para 
    /// a renderização das tabelas após a decodificação do Base64 do WCF/SOAP.
    /// </summary>
    public class PessoaModel
    {
        public int IdPessoa { get; set; }

        [Required(ErrorMessage = "A Razão Social / Nome Completo é obrigatória.")]
        [Display(Name = "Razão Social / Nome")]
        public string RazaoSocial { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF ou CNPJ é obrigatório.")]
        [Display(Name = "CPF / CNPJ")]
        public string CnpjCpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Insira um formato de e-mail válido.")]
        [Display(Name = "E-mail de Contato")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [Display(Name = "Nome de Usuário")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha de segurança é obrigatória.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha de Acesso")]
        public string Senha { get; set; } = string.Empty;
    }
}
