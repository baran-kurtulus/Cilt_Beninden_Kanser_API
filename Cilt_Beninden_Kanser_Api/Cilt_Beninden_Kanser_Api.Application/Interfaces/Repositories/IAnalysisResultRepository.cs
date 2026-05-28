using Cilt_Beninden_Kanser_Api.Domain.Entities;

namespace Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;

public interface IAnalysisResultRepository
{
    Task AddAsync(AnalysisResult result, CancellationToken cancellationToken = default);
    Task<AnalysisResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnalysisResult>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
