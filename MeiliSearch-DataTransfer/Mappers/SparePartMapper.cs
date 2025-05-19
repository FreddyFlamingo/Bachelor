using TransferToMeiliSearch.DtoModels;
using TransferToMeiliSearch.DocModels;

namespace TransferToMeiliSearch.Mappers
{
    public static class SparePartMapper
    {
        public static SparePartDoc ToDoc(SparePartDto dto)
        {
            if (dto == null) return null;

            return new SparePartDoc
            {
                // SparePartId skal konverteres fra Guid til String
                SparePartId = dto.SparePartId.ToString(),
                SparePartPartNo = dto.SparePartPartNo,
                SparePartSerialCode = dto.SparePartSerialCode,
                SparePartName = dto.SparePartName,
                SparePartDescription = dto.SparePartDescription,
                SparePartTypeNo = dto.SparePartTypeNo,
                SparePartNotes = dto.SparePartNotes,

                UnitNo = dto.UnitNo,
                UnitName = dto.UnitName,
                UnitDescription = dto.UnitDescription,

                CategoryNo = dto.CategoryNo,
                CategoryName = dto.CategoryName,
                CategoryDescription = dto.CategoryDescription,

                SupplierNo = dto.SupplierNo,
                SupplierName = dto.SupplierName,
                SupplierNotes = dto.SupplierNotes,

                LocationNo = dto.LocationNo,
                LocationName = dto.LocationName,
                LocationArea = dto.LocationArea,
                LocationBuilding = dto.LocationBuilding,
                LocationNotes = dto.LocationNotes,

                ManufacturerNo = dto.ManufacturerNo,
                ManufacturerName = dto.ManufacturerName,
                ManufacturerNotes = dto.ManufacturerNotes
            };
        }

        public static IEnumerable<SparePartDoc> ToDocs(IEnumerable<SparePartDto> dtos)
        {
            if (dtos == null) return Enumerable.Empty<SparePartDoc>();
            return dtos.Select(ToDoc).Where(doc => doc != null);
        }
    }
}