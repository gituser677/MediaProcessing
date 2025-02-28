using MP.Core.Application.IService;
using MP.Core.Application.Services;
using MP.Core.Domain.Interfaces;
using MP.Infrastructure.Repositories;
using Umbraco.Cms.Core.Composing;

namespace MediaProcessing.Composers;

public class RegisterServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
       
        builder.Services.AddTransient<IImportContentRepository, ImportContentRepository>();
        //builder.Services.AddTransient<IImportContentService, ImportContentService>();
        //builder.Services.AddAutoMapper(typeof(ProfileMapper));
    }
}
