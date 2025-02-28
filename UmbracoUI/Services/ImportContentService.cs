using AutoMapper;
using MediaProcessing.umbraco.models;
using MP.Core.Application.IService;
using MP.Core.Domain.Interfaces;

namespace MP.Core.Application.Services;

public class ImportContentService:IImportContentService
{
    private readonly IMapper _mapper;
    private readonly IImportContentRepository _importContentRepository;
    public ImportContentService(IMapper mapper,IImportContentRepository importContentRepository)
    {
        _mapper = mapper;
        _importContentRepository = importContentRepository;
    }
    public List<ImageFileViewModel> GetMediaList()
    {
        var medialist = _importContentRepository.GetMediaList();
        var viewModelList = _mapper.Map<List<ImageFileViewModel>>(medialist);
        return viewModelList;
    }
}
