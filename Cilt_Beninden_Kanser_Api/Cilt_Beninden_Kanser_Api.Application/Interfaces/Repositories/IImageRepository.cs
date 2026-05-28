using Cilt_Beninden_Kanser_Api.Domain.Entities;

namespace Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;

public interface IImageRepository
{
    Task AddAsync(ImageRecord imageRecord, CancellationToken cancellationToken = default);
}
