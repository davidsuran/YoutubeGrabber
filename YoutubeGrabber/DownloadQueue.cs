namespace YoutubeGrabber
{
    internal class DownloadQueue
    {
        public static DownloadQueue Instance => _lazy.Value;

        private static readonly Lazy<DownloadQueue> _lazy =
            new Lazy<DownloadQueue>(() => new DownloadQueue(), LazyThreadSafetyMode.ExecutionAndPublication);

        private Queue<string> _queuedVs;
        private string _quefile;

        private const string HEADER = "YoutubeVParameter";
        
        private DownloadQueue()
        {
            _queuedVs = new Queue<string>();
            _quefile = Path.Combine(Configuration.Instance.Settings.Paths.Queue, "Queue.csv");

            Load();
        }

        internal string SafeDequeue()
        {
            if (_queuedVs.Count > 0)
            {
                return _queuedVs.Dequeue();
            }

            return string.Empty;
        }

        internal void UpdateFile()
        {
            File.WriteAllLines(
                _quefile,
                (new string[] { HEADER }).Concat(_queuedVs.AsEnumerable()));
        }

        internal void Load()
        {
            using (StreamReader r = new StreamReader(_quefile))
            {
                string raw = r.ReadToEnd();

                foreach (string? v in raw.Split(Environment.NewLine).Skip(1))
                {
                    _queuedVs.Enqueue(v);
                }
            }
        }
    }
}
