using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using ElasticTest.Config;
using ElasticTest.Models;
using Microsoft.Extensions.Options;

namespace ElasticTest.Services
{
    public class AlertElasticService
    {
        private readonly ElasticsearchClient _alertsClient;
        private readonly string _alertsIndex;

        private ElasticsearchClient _client => _alertsClient;
        private string _indexName => _alertsIndex;
        public AlertElasticService(IOptions<ElasticOptions> options)
        {
            var cfg = options.Value;


            // Historical Alerts client
            var alertSettings = new ElasticsearchClientSettings(new Uri(cfg.HistoricalAlerts.Uri))
                .Authentication(new BasicAuthentication(cfg.HistoricalAlerts.Username, cfg.HistoricalAlerts.Password));
            _alertsClient = new ElasticsearchClient(alertSettings);
            _alertsIndex = cfg.HistoricalAlerts.Index;
        }


        // ---------- Search ----------
        public sealed record HistoricalAlertSearchRequest(
            string? q,
            string? truckPlate,
            string? salesOrder,
            string? orderStatus,
            string? customerNo,
            string? productCode,
            DateTime? from,            // filters datetime_stamp >= from (UTC)
            DateTime? to,              // filters datetime_stamp <= to   (UTC)
            string? sortBy,            // e.g. "datetime_stamp"
            string? sortDir,           // "asc" | "desc"
            int page = 1,
            int pageSize = 20
        );

        public sealed record PagedResult<T>(IReadOnlyCollection<T> items, long total, int page, int pageSize);

        public async Task<PagedResult<HistoricalAlertDto>> SearchHistoricalAsync(
           HistoricalAlertSearchRequest req,
           CancellationToken ct = default)
        {
            var must = new List<Query>();
            var filter = new List<Query>();

            if (!string.IsNullOrWhiteSpace(req.q))
            {
                must.Add(new MultiMatchQuery
                {
                    Query = req.q,
                    Fields = new[]
                    {
                        "truck_Plate^3",
                        "plate_letters",
                        "plate_number",
                        "customerName",
                        "productDescription",
                        "plantName",
                        "shipToPartyCustomerName",
                        "truck_status",
                        "orderStatus"
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(req.truckPlate))
                filter.Add(new TermQuery { Field = new Field("truck_Plate.keyword"), Value = req.truckPlate });

            if (!string.IsNullOrWhiteSpace(req.salesOrder))
                filter.Add(new TermQuery { Field = new Field("salesOrder.keyword"), Value = req.salesOrder });

            if (!string.IsNullOrWhiteSpace(req.orderStatus))
                filter.Add(new TermQuery { Field = new Field("orderStatus.keyword"), Value = req.orderStatus });

            if (!string.IsNullOrWhiteSpace(req.customerNo))
                filter.Add(new TermQuery { Field = new Field("customerNo.keyword"), Value = req.customerNo });

            if (!string.IsNullOrWhiteSpace(req.productCode))
                filter.Add(new TermQuery { Field = new Field("productCode.keyword"), Value = req.productCode });

            if (req.from is not null || req.to is not null)
            {
                var dr = new DateRangeQuery(new Field("datetime_stamp"));
                if (req.from is not null) dr.Gte = req.from;
                if (req.to is not null) dr.Lte = req.to;
                filter.Add(dr);
            }

            var sortField = string.IsNullOrWhiteSpace(req.sortBy) ? "datetime_stamp" : req.sortBy!;
            var sortOrder = string.Equals(req.sortDir, "asc", StringComparison.OrdinalIgnoreCase)
                ? SortOrder.Asc : SortOrder.Desc;

            var from = (Math.Max(1, req.page) - 1) * Math.Max(1, req.pageSize);
            var size = Math.Clamp(req.pageSize, 1, 100);

            // Query ES using the entity model
            //var resp = await _alertsClient.SearchAsync<HistoricalAlertDto>(s => s
            //    .Index(_alertsIndex)
            //    .From(from)
            //    .Size(size)
            //    .Query(q => q.Bool(b => b.Must(must.ToArray()).Filter(filter.ToArray())))
            //    .Sort(so => so.Field(new Field(sortField), sortOrder)),
            //    ct);

            var resp = await _alertsClient.SearchAsync<HistoricalAlertDto>(s => s
    .Index("historical-alerts")
    .From(from)
    .Size(size)
//  .Sort(so => so.Field(new Field(sortField), sortOrder))
, ct);

            if (!resp.IsValidResponse) throw new Exception(resp.DebugInformation);

            // ✅ Map entity -> DTO
            var items = resp.Documents.Select(ToDto).ToList();

            return new PagedResult<HistoricalAlertDto>(items, resp.Total, req.page, req.pageSize);
        }

        private static HistoricalAlertDto ToDto(HistoricalAlertDto e) => new HistoricalAlertDto
        {
            Truck_Plate = e.Truck_Plate,
            Sequence_Number = e.Sequence_Number,
            Plate_Letters = e.Plate_Letters,
            Plate_Number = e.Plate_Number,
            Speed = e.Speed,
            Load_Weight = e.Load_Weight,
            Datetime_Stamp = e.Datetime_Stamp,

            // keep raw XY
            X = e.X,
            Y = e.Y,

            // geo_point to DTO
            Location = e.Location is null ? null : new GeoPointDto
            {
                Latitude = e.Location.Latitude,
                Longitude = e.Location.Longitude
            },

            Truck_Geotagg = e.Truck_Geotagg,
            Sysinsert_Time = e.Sysinsert_Time,
            Truck_Status = e.Truck_Status,
            SalesOrder = e.SalesOrder,
            CustomerNo = e.CustomerNo,
            CustomerName = e.CustomerName,
            CustomerClass = e.CustomerClass,
            ProductCode = e.ProductCode,
            ProductDescription = e.ProductDescription,
            PlantCode = e.PlantCode,
            PlantName = e.PlantName,
            LoadingDate = e.LoadingDate,
            LoadingTime = e.LoadingTime,
            ActualQuantity = e.ActualQuantity,
            UnitOfMeasurement = e.UnitOfMeasurement,
            ShipToPartyCustomerNumber = e.ShipToPartyCustomerNumber,
            ShipToPartyCustomerName = e.ShipToPartyCustomerName,
            ShipToPartyCustomerCity = e.ShipToPartyCustomerCity,
            ShipToPartyCustomerGIS = e.ShipToPartyCustomerGIS,
            TruckNo = e.TruckNo,
            LisenseNo = e.LisenseNo,
            CustomerCommercialNo = e.CustomerCommercialNo,
            ShipToPartyCommercialNo = e.ShipToPartyCommercialNo,
            Carrier_Name_Ar = e.Carrier_Name_Ar,
            Carrier_Name_Eng = e.Carrier_Name_Eng,
            Carrier_Id_No = e.Carrier_Id_No,
            Carrier_Org_Lic_No = e.Carrier_Org_Lic_No,
            Carrier_Phone = e.Carrier_Phone,
            OffLoadTime = e.OffLoadTime,
            OrderStatus = e.OrderStatus,
            ObjectId = e.ObjectId,
            GlobalId = e.GlobalId
        };



        #region Add

        public static List<HistoricalAlertDto> GenerateTestData(int count = 10)
        {
            var rnd = new Random();
            var list = new List<HistoricalAlertDto>();

            for (int i = 1; i <= count; i++)
            {
                list.Add(new HistoricalAlertDto
                {
                    Truck_Plate = $"PLT-{rnd.Next(100, 999)}",
                    Sequence_Number = i.ToString(),
                    Plate_Letters = "ABC",
                    Plate_Number = rnd.Next(1000, 9999).ToString(),
                    Speed = rnd.Next(0, 120).ToString(),
                    Load_Weight = rnd.Next(3000, 5000).ToString(),
                    Datetime_Stamp = DateTime.UtcNow.AddMinutes(-rnd.Next(0, 5000)),

                    X = (46 + rnd.NextDouble()).ToString("F6"),
                    Y = (24 + rnd.NextDouble()).ToString("F6"),

                    Location = new GeoPointDto
                    {
                        Latitude = 24 + rnd.NextDouble(),
                        Longitude = 46 + rnd.NextDouble()
                    },

                    Truck_Geotagg = $"POINT({46 + rnd.NextDouble()} {24 + rnd.NextDouble()})",
                    Sysinsert_Time = DateTime.UtcNow,
                    Truck_Status = rnd.Next(0, 2) == 0 ? "IN_TRANSIT" : "PARKED_ENGINE_OFF",
                    SalesOrder = $"SO-{rnd.Next(10000, 99999)}",
                    CustomerNo = $"CUST-{rnd.Next(100, 999)}",
                    CustomerName = $"Customer {i}",
                    CustomerClass = rnd.Next(0, 2) == 0 ? "VIP" : "Regular",
                    ProductCode = $"P-{rnd.Next(10, 99)}",
                    ProductDescription = $"Product {rnd.Next(1, 50)}",
                    PlantCode = $"PL-{rnd.Next(1, 10)}",
                    PlantName = $"Plant {rnd.Next(1, 5)}",
                    LoadingDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                    LoadingTime = DateTime.UtcNow.ToString("HH:mm"),
                    ActualQuantity = rnd.Next(1000, 4000).ToString(),
                    UnitOfMeasurement = "TON",
                    ShipToPartyCustomerNumber = rnd.Next(100, 999).ToString(),
                    ShipToPartyCustomerName = $"Warehouse {i}",
                    ShipToPartyCustomerCity = "Riyadh",
                    ShipToPartyCustomerGIS = $"POINT({46 + rnd.NextDouble()} {24 + rnd.NextDouble()})",
                    TruckNo = $"TRK-{rnd.Next(1000, 9999)}",
                    LisenseNo = $"LIC-{rnd.Next(1000, 9999)}",
                    CustomerCommercialNo = $"CR-{rnd.Next(10000, 99999)}",
                    ShipToPartyCommercialNo = $"CR-{rnd.Next(10000, 99999)}",
                    Carrier_Name_Ar = "شركة النقل",
                    Carrier_Name_Eng = "Transport Co",
                    Carrier_Id_No = rnd.Next(1000000000, 1999999999).ToString(),
                    Carrier_Org_Lic_No = $"LIC-{rnd.Next(1000, 9999)}",
                    Carrier_Phone = $"+9665{rnd.Next(10000000, 99999999)}",
                    OffLoadTime = DateTime.UtcNow.AddHours(rnd.Next(1, 10)),
                    OrderStatus = rnd.Next(0, 2) == 0 ? "Delivered" : "In Progress",
                    ObjectId = i,
                    GlobalId = Guid.NewGuid().ToString()
                });
            }

            return list;
        }
        public async Task<int> BulkIndexAsync( CancellationToken ct = default)
        {
            var testData = GenerateTestData(20);

            // Bulk insert into ES
            var bulkResponse = await _alertsClient.BulkAsync(b => b
                .Index("historical-alerts")
                .IndexMany(testData));

            if (!bulkResponse.IsValidResponse)
                Console.WriteLine(bulkResponse.DebugInformation);

                 var failed = bulkResponse.Items
            .Where(i => i.Status is < 200 or >= 300)
            .Select(i => $"{i.Operation} {_indexName}/{i.Id}: {i.Error?.Type} - {i.Error?.Reason}")
            .ToList();

            if (failed.Count > 0)
                throw new Exception("Bulk had item failures: " + string.Join(" | ", failed));

            return bulkResponse.Items.Count(i => i.Status is >= 200 and < 300);
        }
        #endregion
    


        public async Task UnblockIndexAsync(CancellationToken ct = default)
        {
            var resp = await _client.Indices.PutSettingsAsync(
                _indexName,
                ps => ps.Settings(s => s
                    .Blocks(b => b.ReadOnlyAllowDelete(false))
                ),
                ct
            );

            if (!resp.IsValidResponse)
                throw new Exception("Failed to clear read-only block: " + resp.DebugInformation);
        }

    }
}
