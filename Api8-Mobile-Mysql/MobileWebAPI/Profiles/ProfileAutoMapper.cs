using AutoMapper;
using MobileWebAPI.DTO;
using MobileWebAPI.Models;

namespace MobileWebAPI.Profiles
{
    public class ProfileAutoMapper : Profile
    {
        public ProfileAutoMapper()
        {
            CreateMap<Pessoa, PessoaListDTO>();
        }
    }
}
