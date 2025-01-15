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


namespace devMobile.IoT.RTSPCameraFFMpegCore
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
         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
                 .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            // Ensure output directory exists
            Directory.CreateDirectory(_applicationSettings.ImageFilepathLocal);

            // FFmpeg command to read from RTSP stream and save each frame as a JPEG await
            await FFMpegArguments.FromUrlInput(new Uri(_applicationSettings.RtspCameraUrl))
               .OutputToFile($"{_applicationSettings.ImageFilepathLocal}/frame_%06d.jpg",
               false,
               options => options.WithVideoCodec("mjpeg")
               .WithFramerate(1))
               // Adjust frame rate as needed
               .ProcessAsynchronously();
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
   }
}