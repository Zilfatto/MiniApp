using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using MiniApp.Core.Repositories;
using MiniApp.Dtos;

namespace MiniApp.Persistence.Repositories;

public class PairRepository : RepositoryBase, IPairRepository
{
    public async Task<IEnumerable<ReadPairDto>> GetAllAsync(int? code, string? value)
    {
        var queryString = "SELECT Id, Code, Value FROM Pairs";

        if (code.HasValue && value is not null)
        {
            queryString += " WHERE Code = @Code AND Value = @Value";
        }
        else if (code.HasValue)
        {
            queryString += " WHERE Code = @Code";
        }
        else if (value is not null)
        {
            queryString += " WHERE Value = @Value";
        }

        await using var command = SqlServer.CreateCommand(queryString, Connection);

        if (code.HasValue)
        {
            command.Parameters.Add(new SqlParameter("@Code", code.Value));
        }

        if (value is not null)
        {
            command.Parameters.Add(new SqlParameter("@Value", value));
        }

        await TryOpenConnectionAsync();
        
        await using var reader = await command.ExecuteReaderAsync();

        var pairs = new Collection<ReadPairDto>();

        if (!reader.HasRows)
        {
            return pairs;
        }

        while (await reader.ReadAsync())
        {
            pairs.Add(new ReadPairDto
            {
                Id = reader.GetInt64(0),
                Code = reader.GetInt32(1),
                Value = reader.GetString(2)
            });
        }

        return pairs;
    }

    public async Task CreateManyAsync(IEnumerable<CreatePairDto> createPairDtos)
    {
        var dataTable = new DataTable();
        
        dataTable.Columns.Add(new DataColumn("Code", typeof(int)));
        dataTable.Columns.Add(new DataColumn("Value", typeof(string)));
        
        foreach (var createPairDto in createPairDtos)
        {
            dataTable.Rows.Add(createPairDto.Code, createPairDto.Value);
        }
        
        await using var command = SqlServer.CreateCommand("usp_InsertPairs", Connection);

        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TablePairs", dataTable);

        await TryOpenConnectionAsync();
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task ClearAll()
    {
        const string queryString = "TRUNCATE TABLE Pairs";

        await using var command = SqlServer.CreateCommand(queryString, Connection);
        
        await TryOpenConnectionAsync();

        await command.ExecuteNonQueryAsync();
    }
}