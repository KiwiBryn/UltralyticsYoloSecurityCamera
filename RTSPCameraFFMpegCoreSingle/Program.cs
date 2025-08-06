//---------------------------------------------------------------------------------
// Copyright (c) January 2025, devMobile Software
//
// https://mit-license.org/
//
// Thanks https://github.com/rosenbjerg/FFMpegCore/
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;

using FFMpegCore;
using SkiaSharp;


namespace devMobile.IoT.RTSPCameraFFMpegCoreSingle
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static async Task Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} devMobile.IoT.RTSPCameraFFMpegCore starting");
#if RELEASE
         Console.WriteLine("RELEASE");
#else
         Console.WriteLine("DEBUG");
#endif
         // load the app settings into configuration
         var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", false, true)
         .AddUserSecrets<Program>()
         .Build();

         _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

         // Ensure output directory exists
         Directory.CreateDirectory(_applicationSettings.ImageFilepathLocal);

         string filename = Path.Combine(_applicationSettings.ImageFilepathLocal, $"frame_{DateTime.UtcNow:yyMMddHHmmss}.png");

         // Create a command to capture a frame from the RTSP stream
         var frameCaptureCommand = FFMpegArguments
             .FromUrlInput(new Uri(_applicationSettings.RtspCameraUrl))
             .OutputToFile(Path.Combine("image.png"), true, options => options
                 .WithVideoCodec("mjpeg").WithFrameOutputCount(1).WithFramerate(1)
                 .ForceFormat("image2"));

         // Run the command
         frameCaptureCommand.ProcessSynchronously();

         // Load the captured frame into SkiaSharp
         using (var inputStream = System.IO.File.OpenRead("image.png"))
         using (var skStream = new SKManagedStream(inputStream))
         using (var bitmap = SKBitmap.Decode(skStream))
         {
            // Save the frame as a PNG
            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
               using (var fileStream = System.IO.File.OpenWrite(filename))
               {
                  data.SaveTo(fileStream);
               }
            }
         }
      }
   }
}
