//---------------------------------------------------------------------------------
// Copyright (c) November 2023, devMobile Software
// 
// https://www.gnu.org/licenses/#AGPL
//
//---------------------------------------------------------------------------------
using System.Net;

using Microsoft.Extensions.Configuration;

using SkiaSharp;

using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Extensions;
using YoloDotNet.Models;


namespace devMobile.IoT.Ultralytics.YoloDotNetCamera.Performance
{
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "I prefer the old style")]
   class Program
   {
      private static Model.ApplicationSettings? _applicationSettings;
      private static HttpClient? _httpClient;
      private static Yolo? _yolo;
      private static bool _cameraBusy = false;

      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} YoloDotNetHTTPCamera starting");
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

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss} press <ctrl^c> to exit Due:{_applicationSettings.ImageTimerDue} Period:{_applicationSettings.ImageTimerPeriod}");

            NetworkCredential networkCredential = new(_applicationSettings.CameraUserName, _applicationSettings.CameraUserPassword);

            using (_httpClient = new HttpClient(new HttpClientHandler { PreAuthenticate = true, Credentials = networkCredential }))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} CUDA:{_applicationSettings.CUDA} PrimeGPU:{_applicationSettings.PrimeGPU}");

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load start");

               using (_yolo = new Yolo(new YoloOptions()
               {
                  OnnxModel = _applicationSettings.ModelPath,
                  Cuda = _applicationSettings.CUDA,
                  GpuId = _applicationSettings.GPUId,
                  PrimeGpu = _applicationSettings.PrimeGPU,
                  ModelType = ModelType.ObjectDetection,
               }))
               {
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load done");
                  Console.WriteLine();

                  using (Timer imageUpdatetimer = new(ImageUpdateTimerCallback, null, _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod))
                  {
                     try
                     {
                        await Task.Delay(Timeout.Infinite);
                     }
                     catch (TaskCanceledException)
                     {
                        Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutown requested");
                     }
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application failure {ex.Message}", ex);
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
         }
      }


      private static async void ImageUpdateTimerCallback(object? state)
      {
         // Just incase - stop code being called while photo already in progress
         if (_cameraBusy)
         {
            return;
         }
         _cameraBusy = true;

         try
         {
            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Security Camera Image download start");

            using (Stream cameraStream = await _httpClient.GetStreamAsync(_applicationSettings.CameraUrl))
            {
               SKImage image;

               if (_applicationSettings.CameraImageSave)
               {
                  using (Stream fileStream = File.Open(_applicationSettings.CameraImagePath, FileMode.Create))
                  {
                     await cameraStream.CopyToAsync(fileStream);
                  }
                  image = SKImage.FromEncodedData(_applicationSettings.CameraImagePath);

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} Security Camera Image download and save: {_applicationSettings.CameraImagePath}");
               }
               else
               {
                  image = SKImage.FromEncodedData(cameraStream);

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} Security Camera Image download done");
               }

               Console.WriteLine();

               using (image)
               {
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start");

                  var predictions = _yolo.RunObjectDetection(image);

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done");

                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Predictions:{predictions.Count}");

                  foreach (var predicition in predictions)
                  {
                     Console.WriteLine($"  Class {predicition.Label.Name} {(predicition.Confidence * 100.0):f1}% X:{predicition.BoundingBox.Location.X} Y:{predicition.BoundingBox.Location.Y} Width:{predicition.BoundingBox.Width} Height:{predicition.BoundingBox.Height}");
                  }
                  Console.WriteLine();

                  if (_applicationSettings.MarkedUpImageSave)
                  {
                     Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save: {_applicationSettings.MarkedUpImagePath}");

                     using (var markedUpImage = image.Draw(predictions))
                     {
                        markedUpImage.Save(_applicationSettings.MarkedUpImagePath, SKEncodedImageFormat.Jpeg);
                     }
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Security camera image download failed {ex.Message}");
         }
         finally
         {
            _cameraBusy = false;
         }
      }
   }
}
