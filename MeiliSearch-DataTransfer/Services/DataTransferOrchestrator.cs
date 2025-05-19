// TransferToMeiliSearch/Services/DataTransferOrchestrator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            Console.WriteLine($"[{DateTime.Now}] Starting data transfer orchestration (using QueryUnbufferedAsync)...");
            long totalDocumentsSuccessfullySentToMeili = 0;
            int currentSqlOffset = 0;
            bool moreSqlDataExists = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var index = await _meiliService.EnsureIndexExistsAsync(
                    _settings.MeiliSearchIndexUid,
                    _settings.MeiliSearchIndexPrimaryKey,
                    cancellationToken);

                while (moreSqlDataExists && !cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"[{DateTime.Now}] Streaming SQL data. Offset: {currentSqlOffset}, SQL BatchSize (Limit): {_settings.BatchSize}...");

                    var sparePartDocsForCurrentMeiliBatch = new List<SparePartDoc>(_settings.BatchSize);
                    int documentsInThisSqlStream = 0;

                    // Brug await foreach til at konsumere IAsyncEnumerable
                    await foreach (var sparePartDto in _sqlService.StreamSparePartsBatchAsync(currentSqlOffset, _settings.BatchSize, cancellationToken)
                                                                 .WithCancellation(cancellationToken) // Sikrer cancellation under iteration
                                                                 .ConfigureAwait(false)) // Anbefales for library-kode
                    {
                        documentsInThisSqlStream++;
                        var sparePartDoc = SparePartMapper.ToDoc(sparePartDto);
                        if (sparePartDoc != null)
                        {
                            sparePartDocsForCurrentMeiliBatch.Add(sparePartDoc);
                        }
                        // Ingen grund til at tjekke batchstørrelse her
                        // IAsyncEnumerable signalerer slut, når SQL-siden er færdig med den aktuelle page.
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Data transfer was cancelled during SQL data streaming for offset {currentSqlOffset}.");
                        break;
                    }

                    if (documentsInThisSqlStream > 0)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Streamed {documentsInThisSqlStream} spareparts from SQL for offset {currentSqlOffset}.");
                        Console.WriteLine($"[{DateTime.Now}] Sending batch of {sparePartDocsForCurrentMeiliBatch.Count} documents to MeiliSearch...");

                        await _meiliService.ProcessBatchAsync(index, sparePartDocsForCurrentMeiliBatch, cancellationToken);

                        totalDocumentsSuccessfullySentToMeili += sparePartDocsForCurrentMeiliBatch.Count;
                        Console.WriteLine($"[{DateTime.Now}] Batch processed by MeiliSearch. Total documents successfully sent so far: {totalDocumentsSuccessfullySentToMeili}.");
                    }

                    // Bestem om der er mere data
                    if (documentsInThisSqlStream < _settings.BatchSize)
                    {
                        moreSqlDataExists = false;
                        Console.WriteLine($"[{DateTime.Now}] Streamed {documentsInThisSqlStream} documents from SQL (SQL BatchSize was {_settings.BatchSize}). Assuming no more data.");
                    }
                    else if (documentsInThisSqlStream == 0 && currentSqlOffset == 0)
                    {
                        moreSqlDataExists = false;
                        Console.WriteLine($"[{DateTime.Now}] No data found in SQL database from the beginning.");
                    }
                    else if (documentsInThisSqlStream == 0)
                    {
                        moreSqlDataExists = false;
                        Console.WriteLine($"[{DateTime.Now}] No more data found in SQL from offset {currentSqlOffset}.");
                    }
                    else
                    {
                        currentSqlOffset += _settings.BatchSize;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"[{DateTime.Now}] Data transfer was cancelled by user request partway through.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] Data transfer orchestration completed. Total documents sent to MeiliSearch: {totalDocumentsSuccessfullySentToMeili}.");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[{DateTime.Now}] Data transfer operation was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] CRITICAL ERROR during data transfer orchestration: {ex.ToString()}");
            }
        }
    }
}