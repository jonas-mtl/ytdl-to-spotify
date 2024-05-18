using NAudio.Wave;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace yt2mp3
{
    internal class Program
    {
        static VideoId vId;
        static Video video;
        static DateTime startTime;

        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: yt2mp3 <id>");
                return;
            }

            var url = args[0];

            YoutubeClient ytClient = new YoutubeClient();

            try
            {
                vId = VideoId.Parse(url);
                video = await ytClient.Videos.GetAsync(vId);
            } catch 
            {
                Console.WriteLine(args[0] + " - Video not found!");
                Environment.Exit(1);
            }


            if (video.Duration > TimeSpan.FromMinutes(5))
            {
                Console.WriteLine(vId + " - Video duration exeeds 5 minute limit!");
                Environment.Exit(2);
            }

            startTime = DateTime.Now;

            var streamManifest = await ytClient.Videos.Streams.GetManifestAsync(vId);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            var outputFilePath = $"./{video.Title}.mp3";
            var tempFilePath = $"./{video.Title}.tmp";

            Console.WriteLine(vId + " - Downloading... | " + video.Title);


            // Download the audio stream to a temporary file
            await ytClient.Videos.Streams.DownloadAsync(audioStreamInfo, tempFilePath);

            // Convert the downloaded audio to MP3 format
            using (var reader = new MediaFoundationReader(tempFilePath))
            {
                MediaFoundationEncoder.EncodeToMp3(reader, outputFilePath);
            }

            // Delete the temporary file
            System.IO.File.Delete(tempFilePath);

            // Tag the MP3 file with metadata
            var file = TagLib.File.Create(outputFilePath);
            file.Tag.Title = video.Title.ToString().Split('-')[1];
            file.Tag.Performers = new[] { video.Title.ToString().Split('-')[0] }; // Set the artist

            // Download and crop the album art to 1:1 aspect ratio
            var albumArtUrl = $"https://i3.ytimg.com/vi/{vId}/maxresdefault.jpg";
            var albumArtBytes = await new HttpClient().GetByteArrayAsync(albumArtUrl);
            var croppedAlbumArtBytes = CropImageToSquare(albumArtBytes);

            // Add the cropped album art to the MP3 file
            var picture = new TagLib.Id3v2.AttachmentFrame
            {
                Type = PictureType.FrontCover,
                Data = new ByteVector(croppedAlbumArtBytes),
                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
            };
            file.Tag.Pictures = new[] { picture };

            // Save changes
            file.Save();
            TimeSpan duration = DateTime.Now - startTime;
            Console.WriteLine(vId + $" - Finished! ({duration.Milliseconds}ms)");
        }

        // Method to crop the image to a square aspect ratio
        static byte[] CropImageToSquare(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                using (var originalImage = Image.FromStream(ms))
                {
                    int size = Math.Min(originalImage.Width, originalImage.Height);
                    int x = (originalImage.Width - size) / 2;
                    int y = (originalImage.Height - size) / 2;

                    var croppedImage = new Bitmap(size, size);

                    using (var graphics = Graphics.FromImage(croppedImage))
                    {
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;

                        graphics.DrawImage(originalImage, new Rectangle(0, 0, size, size), new Rectangle(x, y, size, size), GraphicsUnit.Pixel);
                    }

                    using (var msCropped = new MemoryStream())
                    {
                        croppedImage.Save(msCropped, ImageFormat.Jpeg);
                        return msCropped.ToArray();
                    }
                }
            }
        }
    }
}