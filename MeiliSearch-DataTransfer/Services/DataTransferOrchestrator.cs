using TransferToMeiliSearch.DtoModels;
using TransferToMeiliSearch.DocModels;
using TransferToMeiliSearch.Mappers;
using TransferToMeiliSearch.Services.Interfaces;

namespace TransferToMeiliSearch.Services
{
    public class DataTransferOrchestrator : IDataTransferOrchestrator
    {
        private readonly ISqlDataService _sqlService;
        private readonly IMeiliSearchService _meiliService;
        private readonly AppSettings _settings;

        public DataTransferOrchestrator(
            ISqlDataService sqlService,
            IMeiliSearchService meiliService,
            AppSettings settings)
        {
            _sqlService = sqlService ?? throw new ArgumentNullException(nameof(sqlService));
            _meiliService = meiliService ?? throw new ArgumentNullException(nameof(meiliService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task OrchestrateTransferAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[{DateTime.Now}] Starting data transfer orchestration...");
            long totalDocumentsProcessed = 0;
            int currentOffset = 0;
            bool moreDataExists = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var index = await _meiliService.EnsureIndexExistsAsync(
                    _settings.MeiliSearchIndexUid,
                    _settings.MeiliSearchIndexPrimaryKey,
                    cancellationToken);

                while (moreDataExists && !cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"[{DateTime.Now}] Fetching SQL data batch. Offset: {currentOffset}, BatchSize: {_settings.BatchSize}...");
                    var sparePartDtos = (await _sqlService.GetSparePartsBatchAsync(currentOffset, _settings.BatchSize, cancellationToken)).ToList();

                    if (!sparePartDtos.Any())
                    {
                        moreDataExists = false;
                        Console.WriteLine($"[{DateTime.Now}] No more data found in SQL to transfer.");
                        return;
                    }

                    Console.WriteLine($"[{DateTime.Now}] Fetched {sparePartDtos.Count()} spareparts from SQL.");

                    var sparePartDocs = SparePartMapper.ToDocs(sparePartDtos).ToList();

                    if (sparePartDocs.Any())
                    {
                        await _meiliService.ProcessBatchAsync(index, sparePartDocs, cancellationToken);
                        totalDocumentsProcessed += sparePartDocs.Count;
                        Console.WriteLine($"[{DateTime.Now}] Processed batch. Total documents processed so far: { totalDocumentsProcessed}.");
                    }
                    currentOffset += _settings.BatchSize;
                }
                
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"[{DateTime.Now}] Data transfer was cancelled during batch processing.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] Data transfer orchestration completed succesfully. Total documents processed: {totalDocumentsProcessed}.");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[{DateTime.Now}] Data transfer was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] CRITICAL ERROR during data transfer orchestration: {ex.ToString()}");
            }
        }
    }
}