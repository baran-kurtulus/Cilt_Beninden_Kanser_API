using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Domain.Entities;

namespace Cilt_Beninden_Kanser_Api.Infrastructure.Persistence.Repositories;

public class ImageRepository : IImageRepository
{
    private readonly AppDbContext _context;

    public ImageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ImageRecord imageRecord, CancellationToken cancellationToken = default)
    {
        _context.ImageRecords.Add(imageRecord);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
