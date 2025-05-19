using TransferToMeiliSearch.DocModels;
using Meilisearch;

namespace TransferToMeiliSearch.Services.Interfaces
{
    public interface IMeiliSearchService
    {
        Task<Meilisearch.Index> EnsureIndexExistsAsync(string indexUid, string primaryKey, CancellationToken cancellationToken = default);
        Task ProcessBatchAsync(Meilisearch.Index index, IEnumerable<SparePartDoc> documents, CancellationToken cancellationToken = default);
    }
}