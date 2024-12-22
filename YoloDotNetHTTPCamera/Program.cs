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


namespace devMobile.IoT.Ultralytics.YoloDotNetCamera
{
   class Program
   {
      private static Model.ApplicationSettings? _applicationSettings;
      private static HttpClient? _httpClient;
      private static Yolo? _yolo;
      private static bool _cameraBusy = false;

      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} SecurityCameraImage starting");
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
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load start");

               using (_yolo = new Yolo(new YoloOptions()
               {
                  OnnxModel = _applicationSettings.ModelPath,
                  Cuda = false,
                  PrimeGpu = false,
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
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutown failure {ex.Message}", ex);
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
            using (Stream fileStream = File.Open(_applicationSettings.ImageInputPath, FileMode.Create))
            {
               await cameraStream.CopyToAsync(fileStream);
            }

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} Security Camera Image download done");

            using (var image = SKImage.FromEncodedData(_applicationSettings.ImageInputPath))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start");

               var predictions = _yolo.RunObjectDetection(image);

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done");
               Console.WriteLine();

               foreach (var predicition in predictions)
               {
                  Console.WriteLine($"  Class {predicition.Label.Name} {(predicition.Confidence * 100.0):f1}% X:{predicition.BoundingBox.Location.X} Y:{predicition.BoundingBox.Location.Y} Width:{predicition.BoundingBox.Width} Height:{predicition.BoundingBox.Height}");
               }
               Console.WriteLine();

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save : {_applicationSettings.ImageOutputPath}");

               using (var output = image.Draw(predictions))
               {
                  output.Save(_applicationSettings.ImageOutputPath, SKEncodedImageFormat.Jpeg);
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
