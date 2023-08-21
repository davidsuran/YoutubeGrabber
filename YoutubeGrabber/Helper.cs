using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;

namespace YoutubeGrabber
{
    public static class Helper
    {
        /// <summary>
        /// Gets the full URL.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns></returns>
        public static string GetFullUrl(string v)
        {
            return FormattableString.Invariant($"https://www.youtube.com/watch?v={v}");
        }

        /// <summary>
        /// Gets the name of the audio file.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns></returns>
        public static string GetAudioFileName(string v)
        {
            return FormattableString.Invariant($"a_{v}");
        }

        /// <summary>
        /// Gets the name of the video file.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns></returns>
        public static string GetVideoFileName(string v)
        {
            return FormattableString.Invariant($"v_{v}");
        }

        /// <summary>
        /// Gets the youtube video title.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static string GetYoutubeVideoTitle(GrabResult result)
        {
            string youtubeVideoTitle = result.Title;

            foreach (char item in Path.GetInvalidFileNameChars())
            {
                youtubeVideoTitle.Replace(item.ToString(), string.Empty);
            }

            if (youtubeVideoTitle != result.Title)
            {
                Console.WriteLine(FormattableString.Invariant($"Original title:{result.Title} had illegal characters"));
                Console.WriteLine(FormattableString.Invariant($"New title is {youtubeVideoTitle}"));
            }

            return youtubeVideoTitle;
        }

        /// <summary>
        /// Chooses the mono media.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public static GrabbedMedia ChooseMonoMedia(GrabResult result, MediaChannels channel)
        {
            List<GrabbedMedia> resources = result.Resources<GrabbedMedia>()
                .Where(m => m.Channels == channel)
                .ToList();

            if (resources.Count == 0)
            {
                throw new InvalidOperationException("No video/audio options were loaded from youtube.");
            }

            // should be best
            return resources[0];
        }
    }
}
