using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cilt_Beninden_Kanser_Api.Infrastructure.Persistence.Repositories;

public class AnalysisResultRepository : IAnalysisResultRepository
{
    private readonly AppDbContext _context;

    public AnalysisResultRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AnalysisResult result, CancellationToken cancellationToken = default)
    {
        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalysisResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisResults
            .AsNoTracking()
            .Include(x => x.Image)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AnalysisResult>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisResults
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
