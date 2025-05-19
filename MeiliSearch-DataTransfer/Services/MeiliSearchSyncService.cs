using Meilisearch; // Giver adgang til MeilisearchClient, Index, MeilisearchApiError, TaskResource, TaskInfoStatus osv.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // Tilføjet for JSON serialisering
using System.Threading;
using System.Threading.Tasks;
using TransferToMeiliSearch.DocModels;
using TransferToMeiliSearch.Services.Interfaces;

namespace TransferToMeiliSearch.Services
{
    public class MeiliSearchSyncService : IMeiliSearchService
    {
        private readonly MeilisearchClient _meiliClient;
        private readonly AppSettings _settings;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public MeiliSearchSyncService(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _meiliClient = new MeilisearchClient(_settings.MeiliSearchHost, _settings.MeiliSearchApiKey);
        }

        public async Task<Meilisearch.Index> EnsureIndexExistsAsync(string indexUid, string primaryKey, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _meiliClient.GetIndexAsync(indexUid, cancellationToken);
            }
            catch (MeilisearchApiError ex) when (ex.Code == "index_not_found")
            {
                Console.WriteLine($"[{DateTime.Now}] Meilisearch index '{indexUid}' not found. Creating with primary key '{primaryKey}'...");
                var taskInfoForCreation = await _meiliClient.CreateIndexAsync(indexUid, primaryKey, cancellationToken);

                await _meiliClient.WaitForTaskAsync(
                    taskInfoForCreation.TaskUid,
                    60000.0, // Timeout
                    1000,    // Interval
                    cancellationToken
                );

                // GetTaskAsync returnerer TaskResource
                TaskResource taskResultAfterCreation = await _meiliClient.GetTaskAsync(taskInfoForCreation.TaskUid, cancellationToken);

                if (taskResultAfterCreation.Status == TaskInfoStatus.Failed)
                {
                    string errorDetails = "Task failed, but no specific error details dictionary was found on TaskResource.";
                    if (taskResultAfterCreation.Error != null && taskResultAfterCreation.Error.Any())
                    {
                        taskResultAfterCreation.Error.TryGetValue("code", out string errCode);
                        taskResultAfterCreation.Error.TryGetValue("message", out string errMessage);
                        taskResultAfterCreation.Error.TryGetValue("type", out string errType);
                        taskResultAfterCreation.Error.TryGetValue("link", out string errLink);

                        errorDetails = $"MeiliErrorCode: {errCode ?? "N/A"}, Type: {errType ?? "N/A"}, Link: {errLink ?? "N/A"}, Message: {errMessage ?? "N/A"}";
                    }
                    Console.WriteLine($"[{DateTime.Now}] ERROR: Meilisearch task {taskInfoForCreation.TaskUid} (CreateIndex) failed. Details: {errorDetails}");
                    // Brug den hentede errMessage i exception, hvis den findes
                    string exceptionMessage = $"Failed to create Meilisearch index '{indexUid}'.";
                    if (taskResultAfterCreation.Error != null && taskResultAfterCreation.Error.TryGetValue("message", out string specificErrorMessage))
                    {
                        exceptionMessage += $" Meilisearch Task Error: {specificErrorMessage}";
                    }
                    else
                    {
                        exceptionMessage += " Unknown Meilisearch task error or no error message provided.";
                    }
                    throw new Exception(exceptionMessage);
                }
                else if (taskResultAfterCreation.Status != TaskInfoStatus.Succeeded)
                {
                    string detailsString = taskResultAfterCreation.Details != null ? JsonSerializer.Serialize(taskResultAfterCreation.Details, _jsonOptions) : "No details provided.";
                    Console.WriteLine($"[{DateTime.Now}] WARNING: Meilisearch task {taskInfoForCreation.TaskUid} (CreateIndex) completed with status '{taskResultAfterCreation.Status}'. Details: \n{detailsString}");
                }

                Console.WriteLine($"[{DateTime.Now}] Meilisearch index '{indexUid}' (Task: {taskInfoForCreation.TaskUid}) handling completed with status: {taskResultAfterCreation.Status}.");
                return await _meiliClient.GetIndexAsync(indexUid, cancellationToken);
            }
            catch (MeilisearchCommunicationError comEx)
            {
                Console.WriteLine($"[{DateTime.Now}] Meilisearch Communication ERROR while ensuring index '{indexUid}': {comEx.Message}. Full error: {comEx.ToString()}");
                throw;
            }
            catch (MeilisearchApiError apiEx) // Fanger API fejl
            {
                // Det her er MeiliSearch's version af HttpStatusCode.
                // apiEx.Message vil indeholde en sammensat besked, der kan inkludere
                // apiEx.Code, .Type, .Link, og den oprindelige Message fra Meilisearch.
                // apiEx.Code er den Meilisearch-specifikke fejlkode.

                Console.WriteLine($"[{DateTime.Now}] Meilisearch API ERROR (Type: {apiEx.GetType().FullName}) while processing request for index '{indexUid}':\n" + // Viser den faktiske exception type
                                  $"  Meili Specific Code: {apiEx.Code}\n" +            // Dette er den vigtige Meili-fejlkode
                                  $"  Overall Exception Message: {apiEx.Message}\n" +   // Dette er den besked, SDK'en har bygget
                                  $"  Stack Trace: {apiEx.ToString()}");                // ToString() giver hele stack trace for dybdegående debugging
                throw; // Re-throw for at lade den overordnede fejlhåndtering vide, at noget gik galt.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] UNEXPECTED ERROR ensuring Meilisearch index '{indexUid}': {ex.ToString()}");
                throw;
            }
        }

        public async Task ProcessBatchAsync(Meilisearch.Index index, IEnumerable<SparePartDoc> documents, CancellationToken cancellationToken = default)
        {
            if (index == null) throw new ArgumentNullException(nameof(index));
            if (documents == null || !documents.Any())
            {
                Console.WriteLine($"[{DateTime.Now}] No documents provided in batch for index '{index.Uid}'. Skipping.");
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] Sending batch of {documents.Count()} documents to Meilisearch index '{index.Uid}'...");
            try
            {
                var taskInfoForBatch = await index.AddDocumentsAsync(documents, primaryKey: _settings.MeiliSearchIndexPrimaryKey, cancellationToken: cancellationToken);

                double timeoutInMillisecondsForBatch = TimeSpan.FromMinutes(5).TotalMilliseconds;
                await _meiliClient.WaitForTaskAsync(
                    taskInfoForBatch.TaskUid,
                    timeoutInMillisecondsForBatch,
                    1000,
                    cancellationToken
                );

                // GetTaskAsync returnerer TaskResource
                TaskResource taskResultAfterBatch = await _meiliClient.GetTaskAsync(taskInfoForBatch.TaskUid, cancellationToken);
                Console.WriteLine($"[{DateTime.Now}] Batch sent to '{index.Uid}'. Task UID: {taskInfoForBatch.TaskUid}, Final Status: {taskResultAfterBatch.Status}");

                if (taskResultAfterBatch.Status == TaskInfoStatus.Failed)
                {
                    string errorDetails = "Task failed, but no specific error details dictionary was found on TaskResource.";
                    if (taskResultAfterBatch.Error != null && taskResultAfterBatch.Error.Any())
                    {
                        taskResultAfterBatch.Error.TryGetValue("code", out string errCode);
                        taskResultAfterBatch.Error.TryGetValue("message", out string errMessage);
                        taskResultAfterBatch.Error.TryGetValue("type", out string errType);
                        taskResultAfterBatch.Error.TryGetValue("link", out string errLink);

                        errorDetails = $"MeiliErrorCode: {errCode ?? "N/A"}, Type: {errType ?? "N/A"}, Link: {errLink ?? "N/A"}, Message: {errMessage ?? "N/A"}";
                    }
                    Console.WriteLine($"[{DateTime.Now}] ERROR: Meilisearch task {taskInfoForBatch.TaskUid} (AddDocuments) failed. Details: {errorDetails}");
                    // Overvej at kaste en exception eller håndtere fejlen mere specifikt **************
                }
                else if (taskResultAfterBatch.Status != TaskInfoStatus.Succeeded)
                {
                    string detailsString = taskResultAfterBatch.Details != null ? JsonSerializer.Serialize(taskResultAfterBatch.Details, _jsonOptions) : "No details provided.";
                    Console.WriteLine($"[{DateTime.Now}] WARNING: Meilisearch task {taskInfoForBatch.TaskUid} (AddDocuments) completed with status '{taskResultAfterBatch.Status}'. Details: \n{detailsString}");
                }
            }
            catch (MeilisearchCommunicationError comEx)
            {
                Console.WriteLine($"[{DateTime.Now}] Meilisearch Communication ERROR sending batch to '{index.Uid}': {comEx.Message}. Full error: {comEx.ToString()}");
                throw;
            }
            catch (MeilisearchApiError apiEx)
            {
                Console.WriteLine($"[{DateTime.Now}] Meilisearch API ERROR (Type: {apiEx.GetType().FullName}) sending batch to '{index.Uid}':\n" + // Viser den faktiske exception type
                                  $"  Meili Specific Code: {apiEx.Code}\n" +            // Dette er den vigtige Meili-fejlkode
                                  $"  Overall Exception Message: {apiEx.Message}\n" +   // Dette er den besked, SDK'en har bygget
                                  $"  Full error: {apiEx.ToString()}");                 // Alt info om fejlen.
                throw; // Re-throw for at lade den overordnede fejlhåndtering vide, at noget gik galt.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] UNEXPECTED ERROR sending batch to '{index.Uid}': {ex.ToString()}");
                throw;
            }
        }
    }
}