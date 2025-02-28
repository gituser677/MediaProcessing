using MediaProcessing.umbraco.models;

namespace MP.Core.Application.IService;

public interface IImportContentService
{
    List<ImageFileViewModel> GetMediaList();
}
