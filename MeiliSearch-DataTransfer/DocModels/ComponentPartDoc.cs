using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransferToMeiliSearch.DocModels
{
    public class ComponentPartDoc
    {
        // ComponentPart table
        public string ComponentPartId { get; set; }           // MeiliSearch Primary Key som string
        public string ComponentPartPosition { get; set; }
        public string ComponentPartNotes { get; set; }

        // Component table
        public string ComponentNo { get; set; }
        public string ComponentCode { get; set; }
        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }

        // Sparepart table
        public string SparePartNo { get; set; }
        public string SparePartName { get; set; }

        // Unit table
        public string UnitNo { get; set; }
        public string UnitName { get; set; }
        public string UnitDescription { get; set; }
    }
}
