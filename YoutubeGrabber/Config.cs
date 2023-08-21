namespace YoutubeGrabber
{
    [Serializable]
    public class Config
    {
        public Settings Settings { get; set; }
    }

    public class Settings
    {
        public Paths Paths { get; set; }
        public bool KeepTempFiles { get; set; }
        public string TestVideoV { get; set; }
    }

    public class Paths
    {
        // full paths
        public string Downloaded { get; set; }
        public string Output { get; set; }
        public string Temp { get; set; }
        public string Queue { get; set; }
        public string Ffmpeg { get; set; }
    }
}
