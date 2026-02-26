using VegaFileConstructor.Models;

namespace VegaFileConstructor.Services;

public interface IGenerationWorkflowService
{
    Task<Generation> CreateAndGenerateAsync(string userId, Guid templateId, Dictionary<string, string> values);
}
