using System.Collections.ObjectModel;
using System.Resources;
using System.Security;
using System.Web;
using Microsoft.VisualBasic.FileIO;

namespace YoutubeGrabber
{
    internal class DownloadedVideos
    {
        public static DownloadedVideos Instance => _lazy.Value;

        private static readonly Lazy<DownloadedVideos> _lazy =
            new Lazy<DownloadedVideos>(() => new DownloadedVideos(), LazyThreadSafetyMode.ExecutionAndPublication);

        private Dictionary<string, (string title, bool audio, bool video, bool merged)> _downloadedDictionary;

        private readonly string _resourcePath;

        private DownloadedVideos()
        {
            _downloadedDictionary = new Dictionary<string, (string title, bool audio, bool video, bool merged)> { };
            _resourcePath = Path.Combine(Configuration.Instance.Settings.Paths.Downloaded, "Downloaded.csv");

            using (TextFieldParser parser = new TextFieldParser(_resourcePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(";");

                bool isHeader = true;
                while (!parser.EndOfData)
                {
                    if (isHeader)
                    {
                        parser.ReadFields();
                        isHeader = false;
                        continue;
                    }

                    string[] fields = parser.ReadFields();

                    if (fields?.Length != 5)
                    {
                        throw new ArgumentException("Csv line needs to have 5 columns");
                    }

                    _downloadedDictionary.Add(fields[0], (fields[1], bool.Parse(fields[2]), bool.Parse(fields[3]), bool.Parse(fields[4])));
                }
            }
        }

        private bool IsNewUrl(string url)
        {
            return !_downloadedDictionary.ContainsKey(url);
        }

        private bool IsTesting(string v)
        {
            return v == Configuration.Instance.Settings.TestVideoV;
        }

        internal bool IsDownloadedAndMerged(string? v)
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                throw new ArgumentNullException(nameof(v));
            }

            if (IsTesting(v))
            {
                return false;
            }

            if (IsNewUrl(v))
            {
                return false;
            }

            (string title, bool audio, bool video, bool merged) fileStates = _downloadedDictionary[v];

            return fileStates.audio
                && fileStates.video
                && fileStates.merged;
        }

        internal void AddMergedVideo(string v, string title)
        {
            if (IsTesting(v))
            {
                return;
            }


            List<string> result = new List<string>
            {
                "YoutubeVParameter;Title;AudioFile;VideoFile;MergedFile"
            };

            _downloadedDictionary.Add(v, (title, true, true, true));

            foreach (KeyValuePair<string, (string title, bool audio, bool video, bool merged)> kvp in _downloadedDictionary)
            {
                result.Add(FormattableString.Invariant(
                    $"{kvp.Key};{kvp.Value.title};{kvp.Value.audio};{kvp.Value.video};{kvp.Value.merged}"));
            }

        }

        internal IEnumerable<(string v, string title)> GetUnfinished(Func<(string title, bool audio, bool video, bool merged), bool> f)
        {
            return _downloadedDictionary.Where(_ => !f(_.Value)).Select(_ => (_.Key, _.Value.title));
        }

        internal void AddTitle(string v, string title)
        {
            if (_downloadedDictionary.ContainsKey(v))
            {
                throw new ArgumentException(FormattableString.Invariant($"Video {v} was already added to downloaded files."));
            }

            _downloadedDictionary.Add(v, (title, false, false, false));
        }

        internal void AddAudio(string v, string youtubeVideoTitle)
        {
            if (!_downloadedDictionary.ContainsKey(v))
            {
                throw new ArgumentException(FormattableString.Invariant($"Video {v} not found in original downloaded files."));
            }

            (string title, bool audio, bool video, bool merged) kvp = _downloadedDictionary[v];
            _downloadedDictionary[v] = (kvp.title, true, kvp.video, kvp.merged);
        }

        internal void VideoDownloaded(string v)
        {
            if (!_downloadedDictionary.ContainsKey(v))
            {
                throw new ArgumentException(FormattableString.Invariant($"Video {v} not found in original downloaded files."));
            }

            (string title, bool audio, bool video, bool merged) kvp = _downloadedDictionary[v];
            _downloadedDictionary[v] = (kvp.title, kvp.video, true, kvp.merged);
        }

        internal void Merged(string v)
        {
            if (!_downloadedDictionary.ContainsKey(v))
            {
                throw new ArgumentException(FormattableString.Invariant($"Video {v} not found in original downloaded files."));
            }

            (string title, bool audio, bool video, bool merged) kvp = _downloadedDictionary[v];
            _downloadedDictionary[v] = (kvp.title, kvp.audio, kvp.video, true);
        }

        internal void Add(string v)
        {
            if (_downloadedDictionary.ContainsKey(v))
            {
                throw new ArgumentException(FormattableString.Invariant($"Video {v} is already in downloaded dictionary."));
            }

            _downloadedDictionary.Add(v, (v, false, false, false));
        }

        internal void Add(string v, Func<(string title, bool audio, bool video, bool merged), (string title, bool audio, bool video, bool merged)> func)
        {
            if (!_downloadedDictionary.ContainsKey(v))
            {
                throw new ArgumentException(FormattableString.Invariant($"Video {v} not found in original downloaded files."));
            }

            _downloadedDictionary[v] = func.Invoke(_downloadedDictionary[v]);          
        }

        /// <summary>
        /// Updates the file on disk.
        /// </summary>
        internal void UpdateFile()
        {
            List<string> result = new List<string>
            {
                "YoutubeVParameter;Title;AudioFile;VideoFile;MergedFile"
            };

            foreach (KeyValuePair<string, (string title, bool audio, bool video, bool merged)> kvp in _downloadedDictionary)
            {
                result.Add(FormattableString.Invariant(
                    $"{kvp.Key};{kvp.Value.title};{kvp.Value.audio};{kvp.Value.video};{kvp.Value.merged}"));
            }

            File.Delete(_resourcePath);
            File.WriteAllLines(_resourcePath, result);
        }
    }
}
