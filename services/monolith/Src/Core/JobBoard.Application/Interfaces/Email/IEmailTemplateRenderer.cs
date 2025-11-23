namespace JobBoard.Application.Interfaces.Email;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync<TModel>(string templateName, TModel model);
}