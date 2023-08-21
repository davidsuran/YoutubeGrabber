using Jint.Runtime.Interop;
using System.Text.Json;

namespace YoutubeGrabber
{
    internal class Configuration
    {
        public static Configuration Instance => _lazy.Value;

        Config _config;

        private static readonly Lazy<Configuration> _lazy =
            new Lazy<Configuration>(() => new Configuration(), LazyThreadSafetyMode.ExecutionAndPublication);

        public Settings Settings => _lazy.Value._config.Settings;

        private Configuration()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectBoolConverter());

            string resourcePath = Path.Combine(Environment.CurrentDirectory, "Resources\\Config.json");

            using (StreamReader r = new StreamReader(resourcePath))
            {
                string json = r.ReadToEnd();
                _config = JsonSerializer.Deserialize<Config>(json, options) ?? throw new FileLoadException("Configuration could not be deserialized.");
            }
        }
    }
}
