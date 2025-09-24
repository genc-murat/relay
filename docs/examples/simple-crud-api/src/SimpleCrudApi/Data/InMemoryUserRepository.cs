using System.Collections.Concurrent;
using System.Threading;
using SimpleCrudApi.Models;

namespace SimpleCrudApi.Data;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId = 1;

    public InMemoryUserRepository()
    {
        // Seed with some initial data
        var seedUser = new User
        {
            Id = _nextId++,
            Name = "Murat Genc",
            Email = "murat@gencmurat.com",
            CreatedAt = DateTime.UtcNow
        };
        _users.TryAdd(seedUser.Id, seedUser);
    }

    public ValueTask<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return ValueTask.FromResult(user);
    }

    public ValueTask<IEnumerable<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var list = new List<User>(pageSize);
        var startId = (page - 1) * pageSize + 1;
        var endId = startId + pageSize - 1;

        for (int id = startId; id <= endId; id++)
        {
            if (_users.TryGetValue(id, out var user))
            {
                list.Add(user);
            }
        }

        return ValueTask.FromResult<IEnumerable<User>>(list);
    }

    public ValueTask<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var id = Interlocked.Increment(ref _nextId) - 1;
        var newUser = user with { Id = id };
        _users.TryAdd(newUser.Id, newUser);
        return ValueTask.FromResult(newUser);
    }

    public ValueTask<User?> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (_users.TryGetValue(user.Id, out var existingUser))
        {
            _users.TryUpdate(user.Id, user, existingUser);
            return ValueTask.FromResult<User?>(user);
        }

        return ValueTask.FromResult<User?>(null);
    }

    public ValueTask<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = _users.TryRemove(id, out _);
        return ValueTask.FromResult(removed);
    }
}