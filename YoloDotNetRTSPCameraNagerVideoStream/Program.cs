//---------------------------------------------------------------------------------
// Copyright (c) March 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/nager/Nager.VideoStream
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;

using Nager.VideoStream;


namespace devMobile.IoT.Ultralytics.YoloDotNetRtspCamera.NagerVideoStream
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;
      private static DateTime FrameLastUtc = DateTime.UtcNow;
      private static int FrameCount = 0;
      private static TimeSpan TimeSinceLastFrameAverage;
      
      static async Task Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} SecurityCamera.Video.Nager.VideoStream starting");
#if RELEASE
         Console.WriteLine("RELEASE");
#else
         Console.WriteLine("DEBUG");
#endif
         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
                 .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            if (!Directory.Exists(_applicationSettings.ImageFilepathLocal))
            {
               Directory.CreateDirectory(_applicationSettings.ImageFilepathLocal);
            }

            var inputSource = new StreamInputSource(_applicationSettings.RtspCameraUrl);

            var cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () => await StartStreamProcessingAsync(inputSource, cancellationTokenSource.Token));

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            cancellationTokenSource.Cancel();
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutdown failure {ex.Message}", ex);
         }

         Console.WriteLine("Press ENTER to exit");
         Console.ReadLine();
      }

      private static async Task StartStreamProcessingAsync(InputSource inputSource, CancellationToken cancellationToken = default)
      {
         Console.WriteLine("Start Stream Processing");
         try
         {
            var client = new VideoStreamClient();

            client.NewImageReceived += NewImageReceived;
#if FFMPEG_INFO_DISPLAY
            client.FFmpegInfoReceived += FFmpegInfoReceived;
#endif
            await client.StartFrameReaderAsync(inputSource, OutputImageFormat.Png, cancellationToken: cancellationToken);

            client.NewImageReceived -= NewImageReceived;
#if FFMPEG_INFO_DISPLAY
            client.FFmpegInfoReceived -= FFmpegInfoReceived;
#endif
            Console.WriteLine("End Stream Processing");
         }
         catch (Exception exception)
         {
            Console.WriteLine($"{exception}");
         }
      }


      private static void NewImageReceived(byte[] imageData)
      {
         DateTime currentTimeUtc = DateTime.UtcNow;

         TimeSpan timeSinceLastFrame = currentTimeUtc - FrameLastUtc;

         FrameLastUtc = currentTimeUtc;

         if (timeSinceLastFrame.TotalMilliseconds < 1000)
         {
            TimeSinceLastFrameAverage += timeSinceLastFrame;
            FrameCount += 1;
         }

         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Image received - Inter frame:{timeSinceLastFrame.TotalMilliseconds:0.0} mSec Average:{(TimeSinceLastFrameAverage.TotalMilliseconds/FrameCount):0.0} mSec");

         File.WriteAllBytes($"{_applicationSettings.ImageFilepathLocal}\\{currentTimeUtc.Ticks}.png", imageData);
      }

#if FFMPEG_INFO_DISPLAY
      private static void FFmpegInfoReceived(string ffmpegStreamInfo)
      {
         Console.WriteLine(ffmpegStreamInfo);
      }
#endif
   }
}

