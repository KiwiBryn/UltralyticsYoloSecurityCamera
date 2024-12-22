//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
// 
// https://www.gnu.org/licenses/#AGPL
//
//---------------------------------------------------------------------------------
using System.Net;

using Microsoft.Extensions.Configuration;

#if GPURELEASE
   using Microsoft.ML.OnnxRuntime;
#endif

using Compunet.YoloV8;

using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;


namespace devMobile.IoT.Ultralytics.YoloV8Camera
{
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "I prefer the old style")]
   class Program
   {
      private static YoloV8Predictor? _predictor;
      private static Model.ApplicationSettings? _applicationSettings;
      private static HttpClient? _httpClient;
      private static bool _cameraBusy = false;

      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} YoloV8HTTPCamera starting");
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
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load: {_applicationSettings.ModelPath}");

            YoloV8Builder builder = new YoloV8Builder();

            builder.UseOnnxModel(File.ReadAllBytes(_applicationSettings.ModelPath));

#if GPURELEASE
            if (_applicationSettings.UseCuda)
            {
               Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Using CUDA");

               using (OrtCUDAProviderOptions cudaProviderOptions = new())
               {
                  Dictionary<string, string> optionKeyValuePairs = new()
                  {
                     //{ "gpu_mem_limit", ""},
                     //{ "arena_extend_strategy", "0: },
                     //{ "cudnn_conv_algo_search", "0"},
                     //{ "do_copy_in_default_stream", "1"},
                     //{ "cudnn_conv_use_max_workspace", "0"},
                     //{ "cudnn_conv1d_pad_to_nc1d" , "0"},
                     //{ "enable_cuda_graph", "0"},
                     { "device_id" , _applicationSettings.DeviceId.ToString()},
                  };

                  cudaProviderOptions.UpdateOptions(optionKeyValuePairs);

                  string options = cudaProviderOptions.GetOptions();

                  options = options.Replace(";", Environment.NewLine);

                  Console.WriteLine($"CUDA Options:");
                  Console.WriteLine(options);

                  builder.UseCuda(cudaProviderOptions);
               }
            }

            if (_applicationSettings.UseTensorrt)
            {
               Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Using TensorRT");

               using (OrtTensorRTProviderOptions tensorRToptions = new())
               {
                  Dictionary<string, string> optionKeyValuePairs = new()
                  {
                     //{ "trt_max_workspace_size", "2147483648" },                    
                     //{ "trt_max_partition_iterations", "1000" },
                     //{ "trt_min_subgraph_size", "1" },

                     //{ "trt_fp16_enable", "1" },
                     //{ "trt_int8_enable", "0" },

                     //{ "trt_int8_calibration_table_name", "" },
                     //{ "trt_int8_use_native_calibration_table", "0" },

                     //{ "trt_dla_enable", "1" },
                     //{ "trt_dla_core", "0" },

                     //{ "trt_timing_cache_enable", "1" },
                     //{ "trt_timing_cache_path", "timingcache/" },

                     { "trt_engine_cache_enable", "1" },
                     { "trt_engine_cache_path", "enginecache/" },

                     //{ "trt_dump_ep_context_model", "1" },
                     //{ "trt_ep_context_file_path", "embedengine/" },

                     //{ "trt_dump_subgraphs", "0" },
                     //{ "trt_force_sequential_engine_build", "0" },

                     { "device_id" , _applicationSettings.DeviceId.ToString()},
                  };

                  tensorRToptions.UpdateOptions(optionKeyValuePairs);

                  string options = tensorRToptions.GetOptions();

                  options = options.Replace(";", Environment.NewLine);

                  Console.WriteLine($"Tensor RT Options:");
                  Console.WriteLine(options);
 
                  builder.UseTensorrt(tensorRToptions);
               }
            }
#endif
            /*            
            builder.WithConfiguration(c =>
            {
               c.Confidence = 0.0f;
               c.IoU = 0.0f;
               c.KeepOriginalAspectRatio = false;
               c.SuppressParallelInference = false ;
            });
            */

            /*
            builder.WithSessionOptions(new Microsoft.ML.OnnxRuntime.SessionOptions()
            {
               EnableCpuMemArena
               EnableMemoryPattern
               EnableProfiling = true,
               ExecutionMode = ExecutionMode.
               GraphOptimizationLevel = GraphOptimizationLevel.
               InterOpNumThreads = 1,
               ProfileOutputPathPrefix = ""
               OptimizedModelFilePath = ""                
            });
            */

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss} press <ctrl^c> to exit Due:{_applicationSettings.ImageTimerDue} Period:{_applicationSettings.ImageTimerPeriod}");

            NetworkCredential networkCredential = new(_applicationSettings.CameraUserName, _applicationSettings.CameraUserPassword);

            using (_httpClient = new HttpClient(new HttpClientHandler { PreAuthenticate = true, Credentials = networkCredential }))
            using (_predictor = builder.Build())
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
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application failure {ex}");
         }

         Console.WriteLine("Press enter to exit");
         Console.ReadLine();
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

               foreach (var prediction in predictions.Boxes)
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

