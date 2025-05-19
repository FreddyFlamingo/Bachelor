using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Runtime.CompilerServices;
using TransferToMeiliSearch.DtoModels;
using TransferToMeiliSearch.Services.Interfaces;

namespace TransferToMeiliSearch.Services
{
    public class SqlDataService : ISqlDataService
    {
        private readonly AppSettings _settings;

        public SqlDataService(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async IAsyncEnumerable<SparePartDto> StreamSparePartsBatchAsync(int offset, int limit, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Evt. overvej at flytte den til en .sql fil og læse den ind, hvis den bliver meget lang.
            string sql = $@"
                SELECT
                    -- Sparepart
                    sp.Id as SparePartId, sp.SparePartNo AS SparePartPartNo, sp.SparePartSerialCode AS SparePartSerialCode,
                    sp.Name AS SparePartName, sp.Description AS SparePartDescription,
                    sp.TypeNo AS SparePartTypeNo, sp.Notes AS SparePartNotes,
                    sp.UnitGuid, sp.CategoryGuid, sp.SupplierGuid, sp.LocationGuid, sp.ManufacturerGuid,    -- Guids bruges til joins
                    -- Unit
                    u.UnitNo as UnitNo, u.Name AS UnitName, u.Description AS UnitDescription, u.Id,         -- Id (Guid) bruges til join
                    -- Category
                    c.CategoryNo, c.Name AS CategoryName, c.Description AS CategoryDescription, c.Id,       -- Id (Guid) bruges til join
                    -- Supplier
                    s.SupplierNo, s.Name AS SupplierName, s.Notes AS SupplierNotes, s.Id,                   -- Id (Guid) bruges til join
                    -- Location
                    l.LocationNo, l.Name AS LocationName, l.Area AS LocationArea, l.Id,                     -- Id (Guid) bruges til join
                    l.Building AS LocationBuilding, l.Notes AS LocationNotes, l.Id,                         -- Id (Guid) bruges til join
                    -- Manufacturer
                    m.ManufacturerNo, m.Name AS ManufacturerName, m.Notes AS ManufacturerNotes, m.Id  -- Id (Guid) bruges til join
                FROM
                    SparePart sp
                LEFT JOIN Unit u ON sp.UnitGuid = u.Id
                LEFT JOIN Category c ON sp.CategoryGuid = c.Id
                LEFT JOIN Supplier s ON sp.SupplierGuid = s.Id
                LEFT JOIN Location l ON sp.LocationGuid = l.Id
                LEFT JOIN Manufacturer m ON sp.ManufacturerGuid = m.Id
                ORDER BY sp.Id
                OFFSET @Offset ROWS
                FETCH NEXT @Limit ROWS ONLY;
            ";
                
            //try
            //{
            using (var db = new SqlConnection(_settings.SqlConnectionString))
            {
                await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                    
                //var command = new CommandDefinition(
                //    sql, 
                //    new { Offset = offset, Limit = limit }, 
                //    cancellationToken: cancellationToken
                //);
                await foreach (var item in db.QueryUnbufferedAsync<SparePartDto>(
                                            sql: sql,
                                            param: new { Offset = offset, Limit = limit },
                                            transaction: null, // Medmindre du bruger en eksplicit transaktion
                                            commandTimeout: null, // Standard Dapper timeout (typisk 30 sekunder)
                                            commandType: CommandType.Text
                                        ).WithCancellation(cancellationToken)
                                        .ConfigureAwait(false))
                {
                    yield return item;
                }
            }
            //}
            //catch (SqlException ex)
            //{
            //    Console.WriteLine($"[{DateTime.Now}] SQL ERROR fetching data: {ex.Message}");
            //    // Overvej mere specifik fejlhåndtering eller re-throw
            //    throw;
            //}
        }
    }
}