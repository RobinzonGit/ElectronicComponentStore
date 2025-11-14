using Dapper;
using ElectronicComponentStore.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ElectronicComponentStore.Repositories;

public interface IComponentRepository
{
    Task<IEnumerable<Component>> GetAllComponentsAsync();
    Task<IEnumerable<Component>> GetComponentsByTypeAsync(string type);
    Task<Component?> GetComponentByIdAsync(int id);
    Task AddComponentAsync(Component component);
    Task UpdateComponentAsync(Component component);
    Task DeleteComponentAsync(int id);
    Task<IEnumerable<ComponentType>> GetComponentTypesAsync();
}

public class ComponentRepository : IComponentRepository
{
    private readonly string _connectionString;

    public ComponentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<Component>> GetAllComponentsAsync()
    {
        using IDbConnection db = new SqlConnection(_connectionString);
        return await db.QueryAsync<Component>(
            "SELECT * FROM Components ORDER BY Type, Name");
    }

    public async Task<IEnumerable<Component>> GetComponentsByTypeAsync(string type)
    {
        using IDbConnection db = new SqlConnection(_connectionString);
        return await db.QueryAsync<Component>(
            "SELECT * FROM Components WHERE Type = @Type ORDER BY Name",
            new { Type = type });
    }

    public async Task<Component?> GetComponentByIdAsync(int id)
    {
        using IDbConnection db = new SqlConnection(_connectionString);
        return await db.QueryFirstOrDefaultAsync<Component>(
            "SELECT * FROM Components WHERE Id = @Id",
            new { Id = id });
    }

    public async Task AddComponentAsync(Component component)
    {
        using IDbConnection db = new SqlConnection(_connectionString);
        var sql = @"INSERT INTO Components (Name, Type, CellNumber, Quantity, DateOfChanges, Datasheet)
                   VALUES (@Name, @Type, @CellNumber, @Quantity, @DateOfChanges, @Datasheet)";
        await db.ExecuteAsync(sql, component);
    }

    public async Task UpdateComponentAsync(Component component)
    {
        using IDbConnection db = new SqlConnection(_connectionString);
        var sql = @"UPDATE Components 
                   SET Name = @Name, Type = @Type, CellNumber = @CellNumber, 
                       Quantity = @Quantity, DateOfChanges = @DateOfChanges, Datasheet = @Datasheet
                   WHERE Id = @Id";
        await db.ExecuteAsync(sql, component);
    }

    public async Task DeleteComponentAsync(int id)
    {
        using IDbConnection db = new SqlConnection(_connectionString);
        await db.ExecuteAsync("DELETE FROM Components WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<ComponentType>> GetComponentTypesAsync()
    {
        using IDbConnection db = new SqlConnection(_connectionString);
        return await db.QueryAsync<ComponentType>(
            "SELECT Type as TypeName, COUNT(*) as Count FROM Components GROUP BY Type ORDER BY Type");
    }
}