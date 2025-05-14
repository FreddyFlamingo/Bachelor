using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeiliSearch_DataTransfer.DtoModels
{
    public class SparePartDto
    {
        // SparePart table
        public Guid SparePartId { get; set; }               // GUID
        public string SparePartPartNo { get; set; }
        public string SparePartSerialCode { get; set; }
        public string SparePartName { get; set; }
        public string SparePartDescription { get; set; }
        public string SparePartTypeNo { get; set; }
        public string SparePartNotes { get; set; }

        // Unit table
        public string UnitNo { get; set; }
        public string UnitName { get; set; }
        public string UnitDescription { get; set; }

        // Category table
        public string CategoryNo { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }

        // Supplier table
        public string SupplierNo { get; set; }
        public string SupplierName { get; set; }
        public string SupplierNotes { get; set; }

        // Location table
        public string LocationNo { get; set; }
        public string LocationName { get; set; }
        public string LocationArea {  get; set; }
        public string LocationBuilding { get; set; }
        public string LocationNotes { get; set; }

        // Manufacturer table
        public string ManufacturerNo { get; set; }
        public string ManufacturerName { get; set; }
        public string ManufacturerNotes { get; set; }
    }
}
