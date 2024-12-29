namespace NatureRemoEInfluxDbExporter.Options
{
    public class NatureRemoOption
    {
        public string AccessToken { get; set; } = string.Empty;

        public int Interval { get; set; } = 60;
    }
}
