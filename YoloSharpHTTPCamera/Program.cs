//---------------------------------------------------------------------------------
// Copyright (c) November 2023, devMobile Software
// 
// https://www.gnu.org/licenses/#AGPL
//
//---------------------------------------------------------------------------------
using System.Net;

using Microsoft.Extensions.Configuration;

using Compunet.YoloSharp;

using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;


namespace devMobile.IoT.Ultralytics.YoloSharpCamera
{
   class Program
   {
      private static YoloPredictor? _predictor;
      private static Model.ApplicationSettings? _applicationSettings;
      private static HttpClient? _httpClient;
      private static bool _cameraBusy = false;

      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} YoloSharpHTTPCamera starting");
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
            using (_predictor = new YoloPredictor(File.ReadAllBytes(_applicationSettings.ModelPath), new YoloPredictorOptions()
            {
               UseCuda = false,
            }))
            {
               using Timer imageUpdatetimer = new(ImageUpdateTimerCallback,null, _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod); // Release works
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

            using (var image = await Image.LoadAsync<Rgba32>(_applicationSettings.ImageInputPath))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start");

               var predictions = await _predictor.DetectAsync(image);

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done");
               Console.WriteLine();

               foreach (var prediction in predictions)
               {
                  Console.WriteLine($" {prediction.Confidence * 100.0:f1}% X:{prediction.Bounds.X} Y:{prediction.Bounds.Y} Width:{prediction.Bounds.Width} Height:{prediction.Bounds.Height}");
               }
               Console.WriteLine();
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