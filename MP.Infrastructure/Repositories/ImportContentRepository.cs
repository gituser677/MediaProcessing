
using CsvHelper;
using MP.Core.Domain.DTOs;
using MP.Core.Domain.Interfaces;
using OfficeOpenXml;
using System.Globalization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common;

namespace MP.Infrastructure.Repositories;

public class ImportContentRepository : IImportContentRepository
{
    private string _msg = "";
    private readonly IMediaService _mediaService;
    private readonly IContentService _contentService;
    private readonly ICoreScopeProvider _coreScopeProvider;
    private readonly UmbracoHelper _umbracoHelper;
    private readonly IPublishedContentQuery _publishedContentQuery;
    public ImportContentRepository(IPublishedContentQuery publishedContentQuery, IMediaService mediaService, IContentService contentService, ICoreScopeProvider coreScopeProvider, UmbracoHelper umbracoHelper)
    {
        _mediaService = mediaService;
        _contentService = contentService;
        _coreScopeProvider = coreScopeProvider;
        _umbracoHelper = umbracoHelper;
        _publishedContentQuery = publishedContentQuery;
    }
    /// <summary>
    /// Get Media list
    /// </summary>
    /// <returns></returns>
    public async Task<List<ImageFile>> GetMediaList()
    {
        var mediaItems = _mediaService.GetRootMedia();

        // Filter the media items to only return images based on file extension or MIME type
        var imageMediaItems = mediaItems
               .Where(m => m.ContentType.Alias == "Image") // Adjust to your media type alias
               .Select(m => new ImageFile
               {
                   Id = m.Id,
                   Name = m.Name,
                   CreatedDate = m.CreateDate,
                   ImageUrl = m.GetValue<string>("umbracoFile") // Assuming the file is stored in this property
               }).ToList();


        return imageMediaItems;
    }
    /// <summary>
    /// This is also common method for create Media image type and save image into media section
    /// </summary>
    /// <param name="sourceFolderPath">file path</param>
    /// <returns></returns>
    public async Task<int> UploadImages(string sourceFolderPath)
    {
        var Msg = "";
        using (var scope = _coreScopeProvider.CreateCoreScope(autoComplete: true))
        {
            try
            {
                FileInfo fileInfo = new FileInfo(sourceFolderPath);
                string fileName = fileInfo.Name;
                var mediaItem = _mediaService.CreateMedia(fileName, -1, "Image");

                using (var stream = System.IO.File.OpenRead(sourceFolderPath))
                {
                    var uniqueFolder = Guid.NewGuid().ToString().Substring(0, 8); // Simulates Umbraco folder generation
                    string mediaPath = $"media/{fileName}";
                    string fullMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", mediaPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullMediaPath));
                    using (var fileStream = new FileStream(fullMediaPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                    mediaItem.SetValue("umbracoFile", $"/{mediaPath}");
                    _mediaService.Save(mediaItem);
                    Msg = "Image files imported successfully";
                }
                return mediaItem.Id;
            }
            catch (Exception ex)
            {
                // _logger.LogError($"Error uploading image: {ex.Message}");
            }
        }
        return 0;
    }
    public async Task<string> ImportViaCsv(string csvFilePath, List<string> imagetypes)
    {

        var records = ReadCsv(csvFilePath);
        IContent? getHomeNode = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "parentPage");
        var articles = _contentService.GetPagedChildren(getHomeNode?.Id ?? 0, 0, int.MaxValue, out _).Where(x => x.ContentType.Alias == "articlePage").FirstOrDefault();
        foreach (var record in records)
        {
            string extension = Path.GetExtension(record.image);
            if (!imagetypes.Contains(extension))
            {
                continue;
            }
            var content = _contentService.GetById(articles?.Id ?? 0);  // assuming ContentId is passed in CSV
            if (content != null)
            {
                var existingContent = _contentService.GetPagedChildren(articles.Id, 0, int.MaxValue, out _)
                                  .FirstOrDefault(x => x.Name == record.title);
                if (existingContent != null)
                {
                    continue;
                }
                string descriptionContent = record.description;

                await SaveContentData(articles, content, articles?.ContentType.Alias ?? "", record.title, record.title, descriptionContent, record.image);
                _msg = "data imported successfully";
            }
        }
        return _msg;
    }
    public async Task<string> ImportviaExcel(string csvFilePath, List<string> imagetypes)
    {
        IContent? getHomeNode = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "parentPage");
        var articles = _contentService.GetPagedChildren(getHomeNode?.Id ?? 0, 0, int.MaxValue, out _).Where(x => x.ContentType.Alias == "articlePage").FirstOrDefault();
        if (System.IO.File.Exists(csvFilePath))
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var package = new ExcelPackage(new FileInfo(csvFilePath));
                var worksheet = package.Workbook.Worksheets.First();
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {

                    var image = worksheet.Cells[row, 3].Text;

                    string extension = Path.GetExtension(image);

                    if (!imagetypes.Contains(extension))
                    {
                        continue;
                    }
                    var content = _contentService.GetById(articles?.Id ?? 0);
                    var title = worksheet.Cells[row, 1].Text;
                    var description = worksheet.Cells[row, 2].Text;

                    if (content != null)
                    {
                        //var date = DateTime.TryParse(worksheet.Cells[row, 3].Text, out DateTime parsedDate) ? parsedDate : DateTime.Now;
                        var existingContent = _contentService.GetPagedChildren(articles.Id, 0, int.MaxValue, out _)
                                         .FirstOrDefault(x => x.Name == title);
                        if (existingContent != null)
                        {
                            continue;
                        }
                        await SaveContentData(articles, content, articles?.ContentType.Alias ?? "", title, title, description, image);
                    }
                }
                _msg = "Data imported successfully!";
            }
            catch (Exception ex)
            {
                _msg = ex.Message;
            }
        }
        return _msg;
    }
    /// <summary>
    /// This is common method for save data 
    /// </summary>
    /// <param name="articles">The method returns a single Content object that represents the first "articlePage" content item from the children of the specified parent node.</param>
    /// <param name="content">The method will return a Content object that represents the content with the given ID</param>
    /// <param name="pageAlieas">alieas name of content page</param>
    /// <param name="name">property alieas</param>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="image"></param>
    /// <returns></returns>
    public async Task<string> SaveContentData(dynamic articles, dynamic content, string pageAlieas, string name, string title, string description, string image)
    {
        content = _contentService.Create(name, (articles?.Id ?? 0), articles?.ContentType.Alias ?? "");
        content.SetValue("title", title);  // Use correct alias
        content.SetValue("description", description);  // Use correct alias
        var mediaId = await UploadImages(image);
        IMedia media = _mediaService.GetById(mediaId);
        if (media != null)
        {
            content.SetValue("image", media.GetUdi());
        }
        _contentService.Save(content);
        _contentService.SaveAndPublish(content);
        return "sucess";
    }
    /// <summary>
    /// Read csv file and convert data into model
    /// </summary>
    /// <param name="filePath">uploaded file path</param>
    /// <returns>model list</returns>
    public IEnumerable<CSVRecord> ReadCsv(string filePath)
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            return csv.GetRecords<CSVRecord>().ToList();
        }
    }


}
