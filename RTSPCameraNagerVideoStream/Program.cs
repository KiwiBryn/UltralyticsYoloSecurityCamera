//---------------------------------------------------------------------------------
// Copyright (c) January 2025, devMobile Software
//
// https://mit-license.org/
//
// Big thanks https://github.com/nager/Nager.VideoStream
//
//---------------------------------------------------------------------------------
using System.Diagnostics;

using Microsoft.Extensions.Configuration;

using Nager.VideoStream;


namespace devMobile.IoT.RTSPCameraNagerVideoStream
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static void Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} devMobile.IoT.RTSPCameraNagerVideoStream starting");
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
            File.WriteAllText(Path.Combine(_applicationSettings.ImageFilepathLocal, $"{DateTime.UtcNow:yyyyMMdd-HHmmss.fff}.txt"), "Start");

            await client.StartFrameReaderAsync(inputSource, OutputImageFormat.Png, cancellationToken: cancellationToken);

            File.WriteAllText(Path.Combine(_applicationSettings.ImageFilepathLocal, $"{DateTime.UtcNow:yyyyMMdd-HHmmss.fff}.txt"), "Finish");

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
         Debug.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} NewImageReceived");

         File.WriteAllBytes( Path.Combine(_applicationSettings.ImageFilepathLocal, $"{DateTime.UtcNow:yyyyMMdd-HHmmss.fff}.png"), imageData);
      }

#if FFMPEG_INFO_DISPLAY
      private static void FFmpegInfoReceived(string ffmpegStreamInfo)
      {
         Console.WriteLine(ffmpegStreamInfo);
      }
#endif
   }
}
