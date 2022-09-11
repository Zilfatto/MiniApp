using MiniApp.Dtos.RequestLog;

namespace MiniApp.Core.Repositories;

public interface IRequestLogRepository : IRepository
{
    Task LogAsync(CreateRequestLogDto createRequestLogDto);
}