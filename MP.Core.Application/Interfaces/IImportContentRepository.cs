using MP.Core.Domain.DTOs;

namespace MP.Core.Domain.Interfaces;

public interface IImportContentRepository
{
   Task<List<ImageFile>> GetMediaList();
   Task<string> ImportViaCsv(string filepath, List<string> imagetypes);
   Task<string> ImportviaExcel(string filepath, List<string> imagetypes);
   Task<int> UploadImages(string sourceFolderPath);
}
