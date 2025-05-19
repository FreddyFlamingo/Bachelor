using TransferToMeiliSearch.DtoModels;

namespace TransferToMeiliSearch.Services.Interfaces
{
    public interface ISqlDataService
    {
        IAsyncEnumerable<SparePartDto> StreamSparePartsBatchAsync(int offset, int limit, CancellationToken cancellationToken = default);
    }
}