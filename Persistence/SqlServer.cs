using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using MiniApp.Common.AppSettings.DataSources;
using MiniApp.Common.UserSecrets;
using MiniApp.Persistence.Migrations;

namespace MiniApp.Persistence;

public static class SqlServer
{
    public static string ConnectionString
    {
        get => _connectionString ?? throw new Exception();
        set => _connectionString = value;
    }
    private static string? _connectionString;

    public static string DatabaseConnectionString
    {
        get => _databaseConnectionString ?? throw new Exception();
        set => _databaseConnectionString = value;
    }
    private static string? _databaseConnectionString;

    public static void Setup(IConfiguration configuration)
    {
        // Set a connection string to SQL Server
        var sqlServerConnectionStringBuilder = new SqlConnectionStringBuilder(configuration.GetConnectionString(SqlServerSettings.ConnectionString))
        {
            Password = configuration[UserSecrets.SqlServerPassword]
        };
        
        ConnectionString = sqlServerConnectionStringBuilder.ConnectionString;
        
        // Set a connection string to SQL Server database
        var sqlServerDatabaseConnectionStringBuilder = new SqlConnectionStringBuilder(configuration.GetConnectionString(SqlServerSettings.DatabaseConnectionString))
        {
            Password = configuration[UserSecrets.SqlServerPassword]
        };

        DatabaseConnectionString = sqlServerDatabaseConnectionStringBuilder.ConnectionString;
    }

    public static SqlConnection CreateConnection() => new(ConnectionString);

    public static SqlConnection CreateDatabaseConnection() => new(DatabaseConnectionString);

    public static SqlCommand CreateCommand() => new();

    public static SqlCommand CreateCommand(string cmdText) => new(cmdText);

    public static SqlCommand CreateCommand(string cmdText, SqlConnection connection) => new(cmdText, connection);

    public static SqlCommand CreateCommand(string cmdText, SqlConnection connection, SqlTransaction transaction) =>
        new(cmdText, connection, transaction);

    public static async Task InitializeDatabaseAsync()
    {
        await using var dbConnection = CreateDatabaseConnection();

        try
        {
            await dbConnection.OpenAsync();
        }
        catch (SqlException)
        {
            await CheckDatabaseAsync(dbConnection);

            await Task.Delay(5000);

            await dbConnection.OpenAsync();
        }

        await CheckMigrationsTable(dbConnection);

        var lastAppliedMigrationDate = await GetLastAppliedMigrationDateAsync(dbConnection);
        
        await dbConnection.CloseAsync();
        
        try
        {
            await ApplyMigrationsAsync(lastAppliedMigrationDate);
            
            // await RemoveMigrationsAsync(null);
        }
        catch (SqlException e)
        {
            Console.WriteLine($"ERROR: {e.Message}");
        }
    }

    private static async Task CheckDatabaseAsync(IDbConnection databaseConnection)
    {
        await using var command = CreateCommand();
        
        command.CommandText = @$"
        IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{databaseConnection.Database}')
            BEGIN
                CREATE DATABASE {databaseConnection.Database}
            END
        ";
        
        await using var sqlServerConnection = CreateConnection();
        
        command.Connection = sqlServerConnection;

        await sqlServerConnection.OpenAsync();
        
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CheckMigrationsTable(SqlConnection connection)
    {
        await using var command = CreateCommand();

        command.CommandText = @$"
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = '{IMigration.DatabaseTableName}')
        BEGIN
            CREATE TABLE {IMigration.DatabaseTableName} (Name VARCHAR(150) NOT NULL, Date DATETIME2 NOT NULL)
        END
        ";

        command.Connection = connection;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<DateTime?> GetLastAppliedMigrationDateAsync(SqlConnection connection)
    {
        await using var command = CreateCommand();

        command.CommandText = @"SELECT TOP(1) Date FROM __Migrations ORDER BY Date DESC";

        command.Connection = connection;

        var reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            return null;
        }

        await reader.ReadAsync();
        
        return reader.GetDateTime(0);
    }

    private static async Task ApplyMigrationsAsync(DateTime? lastAppliedMigrationDate)
    {
        var migrations = GetMigrations()
            .Where(m => !lastAppliedMigrationDate.HasValue || m.Date > lastAppliedMigrationDate)
            .ToList();

        if (migrations.Count == 0)
        {
            return;
        }

        migrations = migrations.OrderBy(m => m.Date).ToList();

        await using var connection = CreateDatabaseConnection();
        
        await connection.OpenAsync();
        
        foreach (var migration in migrations)
        {
            if (!await MigrateAsync(connection, migration, true))
            {
                return;
            }
        }
    }

    private static async Task RemoveMigrationsAsync(IMigration? lastRemainingMigration)
    {
        var migrations = GetMigrations().ToList();

        migrations = migrations.OrderByDescending(m => m.Date).ToList();

        if (lastRemainingMigration is not null)
        {
            var lastRemainingMigrationIndex = migrations.FindIndex(m => m.Name == lastRemainingMigration.Name && m.Date == lastRemainingMigration.Date);
            
            if (lastRemainingMigrationIndex == -1)
            {
                throw new ArgumentException("Invalid parameter values", nameof(lastRemainingMigration));
            }

            migrations = migrations
                .TakeWhile((_, i) => i < lastRemainingMigrationIndex)
                .ToList();
        }

        await using var connection = CreateDatabaseConnection();

        await connection.OpenAsync();
        
        foreach (var migration in migrations)
        {
            if (!await MigrateAsync(connection, migration, false))
            {
                return;
            }
        }
    }

    private static async Task<bool> MigrateAsync(SqlConnection connection, IMigration migration, bool up)
    {
        await using var transaction = connection.BeginTransaction();

        await using var command = CreateCommand();

        command.Connection = connection;
        command.Transaction = transaction;

        if (up)
        {
            migration.Up(command);
        }
        else
        {
            migration.Down(command);
        }
        
        try
        {
            await command.ExecuteNonQueryAsync();

            // Bring state of migrations up to date
            var logCommand = CreateCommand();
            
            logCommand.CommandText = up
                ? $"INSERT INTO __Migrations (Name, Date) VALUES ('{migration.Name}', '{migration.Date}')"
                : $"DELETE FROM __Migrations WHERE Name = '{migration.Name}' AND Date = '{migration.Date}'";
            
            logCommand.Connection = connection;
            logCommand.Transaction = transaction;

            await logCommand.ExecuteNonQueryAsync();
                
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
                
            await transaction.RollbackAsync();

            return false;
        }
    }

    private static IEnumerable<IMigration> GetMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        var types = assembly
            .GetExportedTypes()
            .Where(t => t.GetInterfaces().Any(i => i == typeof(IMigration)))
            .ToList();

        return types
            .Select(Activator.CreateInstance)
            .Where(o => o is IMigration)
            .Cast<IMigration>();
    }
}