using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Categories;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record CategoryDto(Guid Id, string Name, string Color, string Icon, bool IsActive);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

// Get all categories for current tenant
public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCategoriesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return await _context.Categories
            .Where(c => c.TenantId == _currentUser.TenantId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Color, c.Icon, c.IsActive))
            .ToListAsync(ct);
    }
}

// Get category by id
public record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        var c = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        return new CategoryDto(c.Id, c.Name, c.Color, c.Icon, c.IsActive);
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

// Create Category
public record CreateCategoryCommand(string Name, string Color, string Icon) : IRequest<Guid>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(20)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color (e.g. #6366f1)");
        RuleFor(x => x.Icon).NotEmpty().MaximumLength(50);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateCategoryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var exists = await _context.Categories
            .AnyAsync(c => c.TenantId == _currentUser.TenantId &&
                           c.Name.ToLower() == request.Name.ToLower(), ct);

        if (exists)
            throw new InvalidOperationException($"A category named '{request.Name}' already exists.");

        var category = Category.Create(request.Name, _currentUser.TenantId, request.Color, request.Icon);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);
        return category.Id;
    }
}

// Update Category
public record UpdateCategoryCommand(Guid Id, string Name, string Color, string Icon) : IRequest;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(20)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color (e.g. #6366f1)");
        RuleFor(x => x.Icon).NotEmpty().MaximumLength(50);
    }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        category.Update(request.Name, request.Color, request.Icon);
        await _context.SaveChangesAsync(ct);
    }
}

// Delete (soft-deactivate) Category
public record DeleteCategoryCommand(Guid Id) : IRequest;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        // Check if any expenses use this category
        var inUse = await _context.Expenses
            .AnyAsync(e => e.CategoryId == request.Id, ct);

        if (inUse)
        {
            // Soft-delete: just deactivate so existing expenses keep their category
            category.Deactivate();
        }
        else
        {
            _context.Categories.Remove(category);
        }

        await _context.SaveChangesAsync(ct);
    }
}