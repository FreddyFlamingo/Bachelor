using Microsoft.Extensions.DependencyInjection; // Tilføj for DI
using TransferToMeiliSearch.Services;
using TransferToMeiliSearch.Services.Interfaces;

namespace TransferToMeiliSearch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Opsæt Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);

            await using var serviceProvider = services.BuildServiceProvider();

            // Hent orchestrator og kør
            var orchestrator = serviceProvider.GetRequiredService<IDataTransferOrchestrator>();

            Console.WriteLine("Press CTRL+C to cancel the data transfer.");
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) => {
                Console.WriteLine("Cancellation requested...");
                cts.Cancel();
                eventArgs.Cancel = true; // Forhindrer applikationen i at lukke medmindre man selv gør det
            };

            try
            {
                await orchestrator.OrchestrateTransferAsync(cts.Token);
            }
            catch (Exception ex)
            {
                // Dette fanger kun fejl, der ikke er håndteret dybere nede,
                // eller fejl under opsætning/hentning af services.
                Console.WriteLine($"[{DateTime.Now}] UNHANDLED EXCEPTION in Main: {ex.ToString()}");
            }
            Console.WriteLine("Application finished. Press any key to exit.");
            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Registrer AppSettings som en singleton (eller fra konfigurationsfil)
            services.AddSingleton<AppSettings>(); // Vil bruge default constructor værdier

            // Registrer services
            services.AddScoped<ISqlDataService, SqlDataService>();
            services.AddScoped<IMeiliSearchService, MeiliSearchSyncService>();
            services.AddScoped<IDataTransferOrchestrator, DataTransferOrchestrator>();
        }
    }
}