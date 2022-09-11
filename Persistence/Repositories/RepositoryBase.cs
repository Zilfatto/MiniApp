using System.Data;
using System.Data.SqlClient;
using MiniApp.Core.Repositories;

namespace MiniApp.Persistence.Repositories;

public class RepositoryBase : IRepository
{
    protected readonly SqlConnection Connection;

    protected RepositoryBase()
    {
        Connection = SqlServer.CreateDatabaseConnection();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected async Task TryOpenConnectionAsync()
    {
        try
        {
            if (Connection.State is ConnectionState.Closed or ConnectionState.Broken)
            {
                await Connection.OpenAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open connection. {ex.Message}");
            throw;
        }
    }
}