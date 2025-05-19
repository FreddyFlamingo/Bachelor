using TransferToMeiliSearch.DtoModels;

namespace TransferToMeiliSearch.Services.Interfaces
{
    public interface ISqlDataService
    {
        Task<IEnumerable<SparePartDto>> GetSparePartsBatchAsync(int offset, int limit, CancellationToken cancellationToken = default);
    }
}