using MiniApp.Dtos;

namespace MiniApp.Core.Repositories;

public interface IPairRepository : IRepository
{
    Task<IEnumerable<ReadPairDto>> GetAllAsync(int? code, string? value);

    Task CreateManyAsync(IEnumerable<CreatePairDto> createPairDtos);

    Task ClearAll();
}