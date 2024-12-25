using AutoMapper;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Extensions;

public class MappingConfig
{
    public static MapperConfiguration RegisterMaps()
    {
        var mappingConfig = new MapperConfiguration(config =>
        {
            config.CreateMap<User, UserDto>().ReverseMap();

        });
        return mappingConfig;
    }
}
