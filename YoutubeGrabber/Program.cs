using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using System.Diagnostics;
using System.Text;
using File = System.IO.File;

namespace YoutubeGrabber
{
    internal class Program
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly HashSet<string> TempFiles = new HashSet<string>();

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static async Task Main(string[] args)
        {
            Client.Timeout = new TimeSpan(6, 0, 0);

            IMultiGrabber grabber = GrabberBuilder.New()
                .UseDefaultServices()
                .AddYouTube()
                .Build();

            try
            {
                await FinishUnfinishedFiles(grabber);

                string v = DownloadQueue.Instance.SafeDequeue();
                while (!string.IsNullOrWhiteSpace(v))
                {
                    await Download(v, grabber);
                    DownloadQueue.Instance.UpdateFile();
                    DownloadedVideos.Instance.UpdateFile();
                    v = DownloadQueue.Instance.SafeDequeue();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                DownloadedVideos.Instance.UpdateFile();
            }

        }

        /// <summary>
        /// Downloads the specified v.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <param name="grabber">The grabber.</param>
        /// <exception cref="System.InvalidOperationException">
        /// No audio stream detected.
        /// or
        /// No video stream detected.
        /// </exception>
        private async static Task Download(string v, IMultiGrabber grabber)
        {
            if (DownloadedVideos.Instance.IsDownloadedAndMerged(v))
            {
                Console.WriteLine(FormattableString.Invariant($"Url \"{v}\" already downloaded"));
                return;
            }

            GrabResult result = await grabber.GrabAsync(new Uri(Helper.GetFullUrl(v), UriKind.Absolute));

            string youtubeVideoTitle = Helper.GetYoutubeVideoTitle(result);
            Console.WriteLine(FormattableString.Invariant($"Downloading: {youtubeVideoTitle}"));
            
            DownloadedVideos.Instance.Add(v);

            try
            {
                await DownloadAudio(v, result);
                await DownloadVideo(v, result);

                GenerateOutputFile(Helper.GetAudioFileName(v), Helper.GetVideoFileName(v), youtubeVideoTitle, v);


                if (!Configuration.Instance.Settings.KeepTempFiles)
                {

                }
            }
            finally
            {
                foreach (var tempFile in TempFiles)
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Downloads the audio.
        /// </summary>
        /// <param name="youtubeVideoTitle">The youtube video title.</param>
        /// <param name="v">The v.</param>
        /// <param name="result">The result.</param>
        /// <exception cref="System.InvalidOperationException">No audio stream detected.</exception>
        private async static Task DownloadAudio(string v, GrabResult result)
        {
            GrabbedMedia audioStream = Helper.ChooseMonoMedia(result, MediaChannels.Audio);
            if (audioStream == null)
            {
                throw new InvalidOperationException("No audio stream detected.");
            }
            string youtubeVideoTitle = Helper.GetYoutubeVideoTitle(result);
            string audioPath = await DownloadMedia(audioStream, result, Helper.GetAudioFileName(v), Helper.GetYoutubeVideoTitle(result), v);

            DownloadedVideos.Instance.Add(v, f => (youtubeVideoTitle, true, f.video, f.audio));
            DownloadQueue.Instance.UpdateFile();
        }

        /// <summary>
        /// Downloads the video.
        /// </summary>
        /// <param name="youtubeVideoTitle">The youtube video title.</param>
        /// <param name="v">The v.</param>
        /// <param name="result">The result.</param>
        /// <exception cref="System.InvalidOperationException">No video stream detected.</exception>
        private async static Task DownloadVideo(string v, GrabResult result)
        {
            GrabbedMedia videoStream = Helper.ChooseMonoMedia(result, MediaChannels.Video);
            if (videoStream == null)
            {
                throw new InvalidOperationException("No video stream detected.");
            }

            string videoPath = await DownloadMedia(videoStream, result, Helper.GetVideoFileName(v), Helper.GetYoutubeVideoTitle(result), v);

            DownloadedVideos.Instance.Add(v, f => (f.title, f.audio, true, f.audio));
        }

        /// <summary>
        /// Generates the output file.
        /// </summary>
        /// <param name="audioFileName">Name of the audio file.</param>
        /// <param name="videoFileName">Name of the video file.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        /// <param name="v">The v.</param>
        private static void GenerateOutputFile(string audioFileName, string videoFileName, string outputFileName, string v)
        {
            string tempFolderName = FormattableString.Invariant($"{v}_{outputFileName}");
            //TODO:check https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why/39872058#39872058
            //https://superuser.com/questions/277642/how-to-merge-audio-and-video-file-in-ffmpeg
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = Path.Combine(Configuration.Instance.Settings.Paths.Ffmpeg, "ffmpeg.exe");
            startInfo.Arguments = FormattableString.Invariant($"-i \"{Configuration.Instance.Settings.Paths.Temp}\\{tempFolderName}\\{videoFileName}\" -i \"{Configuration.Instance.Settings.Paths.Temp}\\{tempFolderName}\\{audioFileName}\" -c copy \"{Configuration.Instance.Settings.Paths.Output}\\{outputFileName}.mp4\"");
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            Console.WriteLine(string.Format(
                "Executing \"{0}\" with arguments \"{1}\".\r\n",
                startInfo.FileName,
                startInfo.Arguments));

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    process.BeginOutputReadLine();
                    process.WaitForExitAsync();
                    //process.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine($"Output file successfully created.");

            DownloadedVideos.Instance.Add(v, f => (f.title, f.audio, f.video, true));
        }

        /// <summary>
        /// Downloads the media.
        /// </summary>
        /// <param name="media">The media.</param>
        /// <param name="grabResult">The grab result.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="title">The title.</param>
        /// <param name="v">The v.</param>
        /// <returns></returns>
        private static async Task<string> DownloadMedia(GrabbedMedia media, IGrabResult grabResult, string fileName, string title, string v)
        {
            StringBuilder tempFolderName = new StringBuilder();
            char[] invalidChars = Path.GetInvalidPathChars();
            foreach (char c in FormattableString.Invariant($"{v}_{title}"))
            {
                tempFolderName.Append(invalidChars.Contains(c) ? '_' : c);
            }

            Console.WriteLine("Downloading {0}...", media.Title ?? media.FormatTitle ?? media.Resolution);
            using var response = await Client.GetAsync(media.ResourceUri);
            response.EnsureSuccessStatusCode();
            using var downloadStream = await response.Content.ReadAsStreamAsync();
            using var resourceStream = await grabResult.WrapStreamAsync(downloadStream);

            string folderPath = FormattableString.Invariant($"{Configuration.Instance.Settings.Paths.Temp}\\{tempFolderName}");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string path = FormattableString.Invariant($"{folderPath}\\{fileName}");//Path.GetTempFileName();

            using var fileStream = new FileStream(path, FileMode.Create);
            //TempFiles.Add(path);
            await resourceStream.CopyToAsync(fileStream);
            return path;
        }

        /// <summary>
        /// Finishes the unfinished files.
        /// </summary>
        private async static Task FinishUnfinishedFiles(IMultiGrabber grabber)
        {
            //TODO:
            //foreach ((string v, string title) _ in DownloadedVideos.Instance.GetUnfinished(_ => _.audio))
            //{
            //    Download(_.v, grabber);

            //    DownloadedVideos.Instance.UpdateAudio(_.v);
            //}

            foreach ((string v, string title) _ in DownloadedVideos.Instance.GetUnfinished(_ => _.video))
            {
                GrabResult result = await grabber.GrabAsync(new Uri(Helper.GetFullUrl(_.v), UriKind.Absolute));
                await DownloadVideo(_.v, result);
            }

            foreach ((string v, string title) _ in DownloadedVideos.Instance.GetUnfinished(_ => _.merged))
            {
                GenerateOutputFile(Helper.GetAudioFileName(_.v), Helper.GetVideoFileName(_.v), _.title, _.v);
                DownloadedVideos.Instance.Add(_.v, f => (f.title, f.audio, f.video, true));
            }
        }
    }
}
