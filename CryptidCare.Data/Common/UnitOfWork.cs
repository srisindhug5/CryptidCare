using CryptidCare.Claims.Application.Abstractions;
using CryptidCare.Claims.Data.Persistence;
using CryptidCare.Claims.Data.Repositories;

namespace CryptidCare.Claims.Data.Common;

/// <summary>
/// Unit of Work pattern - coordinates multiple repository operations as a single transaction.
/// Ensures data consistency and simplifies transaction management.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Claims repository.</summary>
    IClaimRepository Claims { get; }

    /// <summary>Patients repository.</summary>
    IPatientRepository Patients { get; }

    /// <summary>Medicines repository.</summary>
    IMedicineRepository Medicines { get; }

    /// <summary>Saves all changes made in all repositories as a single transaction.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Begins a database transaction.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the current transaction.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the current transaction.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Unit of Work using Entity Framework Core.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ClaimsDbContext _dbContext;
    private IClaimRepository? _claimRepository;
    private IPatientRepository? _patientRepository;
    private IMedicineRepository? _medicineRepository;

    public UnitOfWork(ClaimsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public IClaimRepository Claims =>
        _claimRepository ??= new ClaimRepository(_dbContext);

    public IPatientRepository Patients =>
        _patientRepository ??= new PatientRepository(_dbContext);

    public IMedicineRepository Medicines =>
        _medicineRepository ??= new MedicineRepository(_dbContext);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
        }
        finally
        {
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
