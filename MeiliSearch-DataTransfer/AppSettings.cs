using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransferToMeiliSearch
{
    public class AppSettings
    {
        public string SqlConnectionString { get; init; } = "Server=localhost;Database=SparePartsDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public string MeiliSearchHost { get; init; } = "http://localhost:7700";
        public string MeiliSearchApiKey { get; init; } = "MIN_EKSTREMT_HEMMELIGE_NOEGLE!321";
        public string MeiliSearchIndexUid { get; init; } = "spareparts";
        public string MeiliSearchIndexPrimaryKey { get; init; } = "SparePartId";
        public int BatchSize { get; init; } = 5000;
    }
}
