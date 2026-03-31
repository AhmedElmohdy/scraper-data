namespace ElasticTest.Models
{
    public sealed class HistoricalAlertDto
    {
        public string? Truck_Plate { get; init; }
        public string? Sequence_Number { get; init; }
        public string? Plate_Letters { get; init; }
        public string? Plate_Number { get; init; }
        public string? Speed { get; init; }
        public string? Load_Weight { get; init; }
        public DateTime? Datetime_Stamp { get; init; }

        // Raw coordinates from attributes (optional)
        public string? X { get; init; }
        public string? Y { get; init; }

        // Canonical point from geometry
        public GeoPointDto? Location { get; init; }  // Lat/Lon

        public string? Truck_Geotagg { get; init; }
        public DateTime? Sysinsert_Time { get; init; }
        public string? Truck_Status { get; init; }
        public string? SalesOrder { get; init; }
        public string? CustomerNo { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerClass { get; init; }
        public string? ProductCode { get; init; }
        public string? ProductDescription { get; init; }
        public string? PlantCode { get; init; }
        public string? PlantName { get; init; }
        public string? LoadingDate { get; init; }
        public string? LoadingTime { get; init; }
        public string? ActualQuantity { get; init; }
        public string? UnitOfMeasurement { get; init; }
        public string? ShipToPartyCustomerNumber { get; init; }
        public string? ShipToPartyCustomerName { get; init; }
        public string? ShipToPartyCustomerCity { get; init; }
        public string? ShipToPartyCustomerGIS { get; init; }
        public string? TruckNo { get; init; }
        public string? LisenseNo { get; init; }
        public string? CustomerCommercialNo { get; init; }
        public string? ShipToPartyCommercialNo { get; init; }
        public string? Carrier_Name_Ar { get; init; }
        public string? Carrier_Name_Eng { get; init; }
        public string? Carrier_Id_No { get; init; }
        public string? Carrier_Org_Lic_No { get; init; }
        public string? Carrier_Phone { get; init; }
        public DateTime? OffLoadTime { get; init; }
        public string? OrderStatus { get; init; }
        public int? ObjectId { get; init; }
        public string? GlobalId { get; init; }
    }

    public sealed class GeoPointDto
    {
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
    }

}
