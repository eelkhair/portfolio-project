using JobBoard.Application.Interfaces.Email;
using RazorLight;

namespace JobBoard.Infrastructure.Smtp;

public class EmailTemplateRenderer(IRazorLightEngine engine) : IEmailTemplateRenderer
{
    // The dependency is "injected" via the constructor

    public async Task<string> RenderAsync<TModel>(string templateName, TModel model)
    {
        return await engine.CompileRenderAsync(templateName, model);
    }
}