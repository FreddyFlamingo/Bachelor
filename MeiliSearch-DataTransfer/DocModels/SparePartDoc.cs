using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TransferToMeiliSearch.DocModels
{
    public class SparePartDoc
    {
        // SparePart table
        [JsonPropertyName("SparePartId")]
        public required string SparePartId { get; set; }               // MeiliSearch Primary Key som string
        public required string SparePartPartNo { get; set; }
        public required string SparePartSerialCode { get; set; }
        public string? SparePartName { get; set; }
        public string? SparePartDescription { get; set; }
        public string? SparePartTypeNo { get; set; }
        public string? SparePartNotes { get; set; }

        // Unit table
        public required string UnitNo { get; set; }
        public required string UnitName { get; set; }
        public string? UnitDescription { get; set; }

        // Category table
        public required string CategoryNo { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryDescription { get; set; }

        // Supplier table
        public required string SupplierNo { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierNotes { get; set; }

        // Location table
        public required string LocationNo { get; set; }
        public string? LocationName { get; set; }
        public string? LocationArea { get; set; }
        public string? LocationBuilding { get; set; }
        public string? LocationNotes { get; set; }

        // Manufacturer table
        public required string ManufacturerNo { get; set; }
        public string? ManufacturerName { get; set; }
        public string? ManufacturerNotes { get; set; }
    }
}
