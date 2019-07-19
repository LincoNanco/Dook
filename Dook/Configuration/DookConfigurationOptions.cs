namespace Dook
{
    public interface IDookConfigurationOptions 
    {
        string ConnectionString { get; set; }
        DbType DatabaseType { get; set; }
        string Suffix { get; set; }
        int? CommandTimeout { get; set; }
    }
    public class DookConfigurationOptions : IDookConfigurationOptions
    {
        public string ConnectionString { get; set; }
        public DbType DatabaseType { get; set; }
        public string Suffix { get; set; } = "Repository";
        public int? CommandTimeout { get; set; }
    }

    public class DookConfigurationOptions<T> : DookConfigurationOptions, IDookConfigurationOptions where T : Context
    {

    }
}