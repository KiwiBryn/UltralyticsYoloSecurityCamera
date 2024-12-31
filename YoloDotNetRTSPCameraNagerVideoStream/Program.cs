//---------------------------------------------------------------------------------
// Copyright (c) December 2024, devMobile Software
//
// https://www.gnu.org/licenses/#AGPL
//
// Thanks https://github.com/nager/Nager.VideoStream
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;

using Nager.VideoStream;

using SkiaSharp;

using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Extensions;
using YoloDotNet.Models;


namespace devMobile.IoT.Ultralytics.YoloDotNetRtspCamera.NagerVideoStream
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;
      private static Yolo? _yolo;
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

            _yolo = new Yolo(new YoloOptions()
            {
               OnnxModel = _applicationSettings.ModelPath,
               Cuda = _applicationSettings.CUDA,
               GpuId = _applicationSettings.GPUId,
               PrimeGpu = _applicationSettings.PrimeGPU,
               ModelType = ModelType.ObjectDetection,
            }); 

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
         finally
         {
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
         }
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

         if (_applicationSettings.Inference)
         {
            using (var image = SKImage.FromEncodedData(imageData))
            {
               var predictions = _yolo.RunObjectDetection(image);

               Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Image received - Predictions:{predictions.Count} Inter frame:{timeSinceLastFrame.TotalMilliseconds:0.0} mSec Average:{(TimeSinceLastFrameAverage.TotalMilliseconds / FrameCount):0.0} mSec");

               if (_applicationSettings.MarkUpImages)
               {
                  using (var markedUpImage = image.Draw(predictions))
                  {
                     markedUpImage.Save($"{_applicationSettings.ImageFilepathLocal}\\{currentTimeUtc.Ticks}.png", SKEncodedImageFormat.Png);
                  }
               }
            }
         }
         else
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Image received - Inter frame:{timeSinceLastFrame.TotalMilliseconds:0.0} mSec Average:{(TimeSinceLastFrameAverage.TotalMilliseconds / FrameCount):0.0} mSec");

            File.WriteAllBytes($"{_applicationSettings.ImageFilepathLocal}\\{currentTimeUtc.Ticks}.png", imageData);
         }
      }

#if FFMPEG_INFO_DISPLAY
      private static void FFmpegInfoReceived(string ffmpegStreamInfo)
      {
         Console.WriteLine(ffmpegStreamInfo);
      }
#endif
   }
}

