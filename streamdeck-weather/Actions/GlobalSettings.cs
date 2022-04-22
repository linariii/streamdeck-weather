namespace Weather.Actions
{
    public class GlobalSettings
    {
        public string ApiKey { get; set; }
        public static GlobalSettings CreateDefaultSettings()
        {
            return new GlobalSettings();
        }   
    }
}