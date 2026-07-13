using MobileWebAPI.DTO;
using MobileWebAPI.Models;

namespace MobileWebAPI.Services
{
    public interface IPessoaInterface
    {
        Task<ResponseModel<List<PessoaListDTO>>> GetPessoaList();
        Task<ResponseModel<PessoaListDTO>> GetPessoaById(int pessoaId);
        Task<ResponseModel<List<PessoaListDTO>>> CreatePessoa(CreatePessoaDTO createPessoaDTO);
        Task<ResponseModel<List<PessoaListDTO>>> AlterPessoa(AlterPessoaDTO alterPessoaDTO);
        Task<ResponseModel<List<PessoaListDTO>>> RemovePessoa(int IdPessoa);

    }
}
