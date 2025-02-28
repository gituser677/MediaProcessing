using Microsoft.AspNetCore.Mvc;
using MP.Core.Domain.Interfaces;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace MediaProcessing.Controllers;

public class ImageController : SurfaceController
{ 
  
    private string _msg="";    
    private readonly IImportContentRepository _importContentRepository; 
    public ImageController(IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,  
       IImportContentRepository importContentRepository
        ) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _importContentRepository = importContentRepository;  
    }

    #region Get Media List
    [HttpGet]
    public IActionResult GetMediaList()
    {
     var medialist= _importContentRepository.GetMediaList(); 
        return Ok(medialist);
    }
    #endregion

    #region Upload image into Media Section
    public IActionResult UploadImagesToUmbraco()
    {
        string sourceFolderPath = @"D:\Images";
        string Msg = "";
        string[] imageFiles = Directory.GetFiles(sourceFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                                       .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg"))
        .ToArray();
        foreach (string sourceFilePath in imageFiles)
        {
            _importContentRepository.UploadImages(sourceFilePath);
        }
        return Ok(Msg);
    }
    #endregion


    [HttpPost]
    public async Task<IActionResult> ImportDataToContent(List<IFormFile> files,List<string> imagetypes)
    {

        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded.");
        }
        
        foreach (var file in files)
        {  
            string fileExtension = Path.GetExtension(file.FileName);
            string tempFilePath = Path.Combine(Path.GetTempPath(), fileExtension); 
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            if (fileExtension == ".csv")
            {
                _msg = await _importContentRepository.ImportViaCsv(tempFilePath, imagetypes);
            }
            else if (fileExtension == ".xlsx") { _msg = await _importContentRepository.ImportviaExcel(tempFilePath, imagetypes); }
        } 
        return Ok(_msg);
    }  
    

}
