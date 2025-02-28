namespace MediaProcessing.umbraco
{
    public class CSVRecord
    {
        public string title { get; set; }
        public string description { get; set; }
        public string image { get; set; } // ID of the related content
    }
}
