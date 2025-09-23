using MinimalApi.Models;

namespace MinimalApi.Services;

public interface IUserRepository
{
    ValueTask<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    ValueTask<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    ValueTask<User?> UpdateAsync(User user, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}