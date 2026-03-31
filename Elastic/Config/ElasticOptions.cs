namespace ElasticTest.Config
{
    public class ElasticOptions
    {
        public ElasticConnection Products { get; set; } = new();
        public ElasticConnection HistoricalAlerts { get; set; } = new();
    }
}
