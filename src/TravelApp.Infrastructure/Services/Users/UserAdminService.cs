using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Abstractions.Users;
using TravelApp.Application.Dtos.Users;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Services.Users;

public sealed class UserAdminService : IUserAdminService
{
    private readonly ITravelAppDbContext _dbContext;

    public UserAdminService(ITravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserAdminDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return users.Select(MapUser).ToList();
    }

    public async Task<UserAdminDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return user is null ? null : MapUser(user);
    }

    public async Task<IReadOnlyList<RoleAdminDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RoleAdminDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAdminDto?> CreateAsync(UpsertUserRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!await IsUniqueAsync(null, request, cancellationToken))
        {
            return null;
        }

        var password = string.IsNullOrWhiteSpace(request.Password)
            ? null
            : BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (password is null)
        {
            return null;
        }

        var user = new User
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = password,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        AttachRoles(user, request.RoleIds);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Guid id, UpsertUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (!await IsUniqueAsync(id, request, cancellationToken))
        {
            return false;
        }

        user.UserName = request.UserName.Trim();
        user.Email = request.Email.Trim();
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        user.UserRoles.Clear();
        AttachRoles(user, request.RoleIds);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> IsUniqueAsync(Guid? currentId, UpsertUserRequestDto request, CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName.Trim();
        var normalizedEmail = request.Email.Trim();

        return !await _dbContext.Users.AnyAsync(x =>
            (!currentId.HasValue || x.Id != currentId.Value) &&
            (x.UserName == normalizedUserName || x.Email == normalizedEmail), cancellationToken);
    }

    private static void AttachRoles(User user, IEnumerable<int> roleIds)
    {
        foreach (var roleId in roleIds.Where(x => x > 0).Distinct())
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
            });
        }
    }

    private static UserAdminDto MapUser(User user)
    {
        return new UserAdminDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            Roles = user.UserRoles
                .Where(x => x.Role is not null)
                .Select(x => new RoleAdminDto
                {
                    Id = x.Role.Id,
                    Name = x.Role.Name,
                    Description = x.Role.Description
                })
                .OrderBy(x => x.Name)
                .ToList()
        };
    }
}
