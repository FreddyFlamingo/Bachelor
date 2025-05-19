namespace TransferToMeiliSearch.Services.Interfaces
{
    public interface IDataTransferOrchestrator
    {
        Task OrchestrateTransferAsync(CancellationToken cancellationToken = default);
    }
}