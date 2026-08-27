namespace GestaoCulto.API.Versioning
{
    public class BuildVersionInfo
    {
        public string Name { get; set; } = "gestaoculto-api";
        public string Version { get; set; } = "0.0.0";
        public string Commit { get; set; } = "unknown";
        public string BuildDate { get; set; } = string.Empty;
        public string Environment { get; set; } = "Production";
    }
}
