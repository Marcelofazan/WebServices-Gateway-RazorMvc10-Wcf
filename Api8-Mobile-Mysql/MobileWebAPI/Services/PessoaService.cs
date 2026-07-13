using AutoMapper;
using MobileWebAPI.DTO;
using MobileWebAPI.Models;
using Dapper;
using MySql.Data.MySqlClient;
using MobileWebAPI.Resources;

namespace MobileWebAPI.Services
{
    public class PessoaService : IPessoaInterface
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public PessoaService(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<ResponseModel<List<PessoaListDTO>>> CreatePessoa(CreatePessoaDTO createPessoaDTO)
        {
            ResponseModel<List<PessoaListDTO>> response = new();

            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var pessoasDatabase = await connection.ExecuteAsync("insert into pessoas (RazaoSocial, CnpjCpf, Email, Telefone, Usuario, Senha ) " +
                                                                      "values (@RazaoSocial, @CnpjCpf, @Email, @Telefone, @Usuario, @Senha)", createPessoaDTO);
                    if (pessoasDatabase == 0)
                    {
                        response.Messages = string.Format(ResponseMessages.INF05);
                        response.Status = false;
                        return response;
                    }

                    var pessoas = await ListPessoas(connection);

                    var mapPessoa = _mapper.Map<List<PessoaListDTO>>(pessoas);

                    response.Data = mapPessoa;
                    response.Messages = string.Format(ResponseMessages.INF04);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ResponseModel<PessoaListDTO>> GetPessoaById(int IdPessoa)
        {
            ResponseModel<PessoaListDTO> response = new();

            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var pessoasDatabase = await connection.QueryFirstOrDefaultAsync<Pessoa>("select * from pessoas where IdPessoa = @Id", new { Id = IdPessoa });

                    if (pessoasDatabase == null)
                    {
                        response.Messages = string.Format(ResponseMessages.INF01);
                        response.Status = false;
                        return response;
                    }

                    var mapPessoa = _mapper.Map<PessoaListDTO>(pessoasDatabase);

                    response.Data = mapPessoa;
                    response.Messages = string.Format(ResponseMessages.INF03);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ResponseModel<List<PessoaListDTO>>> GetPessoaList()
        {
            ResponseModel<List<PessoaListDTO>> response = new();

            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var pessoasDatabase = await connection.QueryAsync<Pessoa>("select * from pessoas");

                    if (pessoasDatabase.Count() == 0)
                    {
                        response.Messages = string.Format(ResponseMessages.INF01);
                        response.Status = false;
                        return response;
                    }

                    var mapPessoa = _mapper.Map<List<PessoaListDTO>>(pessoasDatabase);

                    response.Data = mapPessoa;
                    response.Messages = string.Format(ResponseMessages.INF02);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ResponseModel<List<PessoaListDTO>>> AlterPessoa(AlterPessoaDTO alterPessoaDTO)
        {
            ResponseModel<List<PessoaListDTO>> response = new();

            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var pessoasDatabase = await connection.ExecuteAsync("update pessoas set RazaoSocial = @RazaoSocial, " +
                                                                                        "CnpjCpf = @CnpjCpf, " +
                                                                                        "Email = @Email, " +
                                                                                        "Telefone = @Telefone, " +
                                                                                        "Usuario = @Usuario, " +
                                                                                        "Senha = @Senha where IdPessoa = @IdPessoa ", alterPessoaDTO);

                    if (pessoasDatabase == 0)
                    {
                        response.Messages = string.Format(ResponseMessages.INF06);
                        response.Status = false;
                        return response;
                    }

                    var pessoas = await ListPessoas(connection);

                    var mapPessoa = _mapper.Map<List<PessoaListDTO>>(pessoas);

                    response.Data = mapPessoa;
                    response.Messages = string.Format(ResponseMessages.INF07);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ResponseModel<List<PessoaListDTO>>> RemovePessoa(int IdPessoa)
        {
            ResponseModel<List<PessoaListDTO>> response = new();

            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var pessoasDatabase = await connection.ExecuteAsync("delete from pessoas where IdPessoa = @Id", new { Id = IdPessoa });

                    if (pessoasDatabase == 0)
                    {
                        response.Messages = string.Format(ResponseMessages.INF08);
                        response.Status = false;
                        return response;
                    }

                    var pessoas = await ListPessoas(connection);

                    var mapPessoa = _mapper.Map<List<PessoaListDTO>>(pessoas);

                    response.Data = mapPessoa;
                    response.Messages = string.Format(ResponseMessages.INF09);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static async Task<IEnumerable<Pessoa>> ListPessoas(MySqlConnection connection)
        {
            return await connection.QueryAsync<Pessoa>("select * from pessoas");
        }
    }
}
