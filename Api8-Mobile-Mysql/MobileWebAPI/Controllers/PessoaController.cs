using Microsoft.AspNetCore.Mvc;
using MobileWebAPI.DTO;
using MobileWebAPI.Services;

namespace MobileWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PessoaController : ControllerBase
    {
        private readonly IPessoaInterface _pessoaInterface;
        public PessoaController(IPessoaInterface pessoaInterface)
        {
            _pessoaInterface = pessoaInterface;
        }

        [HttpGet]
        public async Task<IActionResult> GetPessoas()
        {
            var pessoas = await _pessoaInterface.GetPessoaList();

            if (pessoas.Status == false)
            {
                return NotFound(pessoas);
            }

            return Ok(pessoas);
        }

        [HttpGet("{Idpessoa}")]
        public async Task<IActionResult> GetPessoaById(int Idpessoa)
        {
            var pessoas = await _pessoaInterface.GetPessoaById(Idpessoa);

            if (pessoas.Status == false)
            {
                return NotFound(pessoas);
            }

            return Ok(pessoas);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePessoa(CreatePessoaDTO createPessoaDTO)
        {
            var pessoas = await _pessoaInterface.CreatePessoa(createPessoaDTO);

            if (pessoas.Status == false)
            {
                return BadRequest(pessoas);
            }

            return Ok(pessoas);
        }

        [HttpPut]
        public async Task<IActionResult> AlterPessoa(AlterPessoaDTO alterPessoaDTO)
        {
            var pessoas = await _pessoaInterface.AlterPessoa(alterPessoaDTO);

            if (pessoas.Status == false)
            {
                return BadRequest(pessoas);
            }

            return Ok(pessoas);
        }

        [HttpDelete("{IdPessoa}")]
        public async Task<IActionResult> RemovePessoa(int IdPessoa)
        {
            var pessoas = await _pessoaInterface.RemovePessoa(IdPessoa);

            if (pessoas.Status == false)
            {
                return BadRequest(pessoas);
            }

            return Ok(pessoas);
        }
    }
}
