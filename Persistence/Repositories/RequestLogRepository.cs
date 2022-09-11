using System.Data.SqlClient;
using MiniApp.Core.Repositories;
using MiniApp.Dtos.RequestLog;

namespace MiniApp.Persistence.Repositories;

public class RequestLogRepository : RepositoryBase, IRequestLogRepository
{
    public async Task LogAsync(CreateRequestLogDto createRequestLogDto)
    {
        const string queryString = @"INSERT INTO RequestLogs
            (QueryString, Body, Method, Path, Response, Exception) VALUES
            (@QueryString, @Body, @Method, @Path, @Response, @Exception)
        ";

        await using var command = SqlServer.CreateCommand(queryString, Connection);
        command.Parameters.Add(new SqlParameter("@QueryString",
            string.IsNullOrWhiteSpace(createRequestLogDto.QueryString)
                ? DBNull.Value
                : createRequestLogDto.QueryString));
        command.Parameters.Add(new SqlParameter("@Body", string.IsNullOrWhiteSpace(createRequestLogDto.Body)
            ? DBNull.Value
            : createRequestLogDto.Body));
        command.Parameters.Add(new SqlParameter("@Method", createRequestLogDto.Method));
        command.Parameters.Add(new SqlParameter("@Path", createRequestLogDto.Path));
        command.Parameters.Add(new SqlParameter("@Response", string.IsNullOrWhiteSpace(createRequestLogDto.Response)
            ? DBNull.Value
            : createRequestLogDto.Response));
        command.Parameters.Add(new SqlParameter("@Exception", string.IsNullOrWhiteSpace(createRequestLogDto.Exception)
            ? DBNull.Value
            : createRequestLogDto.Exception));
        
        await TryOpenConnectionAsync();

        await command.ExecuteNonQueryAsync();
    }
}