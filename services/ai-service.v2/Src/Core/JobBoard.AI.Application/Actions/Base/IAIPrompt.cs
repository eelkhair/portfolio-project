namespace JobBoard.AI.Application.Actions.Base;

public interface IAiPrompt<in TRequest>
{
    string Name { get; }
    string Version { get; }
    string BuildUserPrompt(TRequest request);
    string BuildSystemPrompt();
    bool AllowTools => true;
}