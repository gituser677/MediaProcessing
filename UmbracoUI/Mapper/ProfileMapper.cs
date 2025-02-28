using AutoMapper;
using MediaProcessing.umbraco.models;
using MP.Core.Domain.DTOs;

namespace MP.Core.Application.Mapper;

class ProfileMapper:Profile
{
    public ProfileMapper()
    {
        CreateMap<ImageFile, ImageFileViewModel>()
           .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)).ReverseMap();

    }
}
