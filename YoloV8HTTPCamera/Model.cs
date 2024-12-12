//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
// 
// https://www.gnu.org/licenses/#AGPL
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Ultralytics.YoloV8Camera.Model
{
   public class ApplicationSettings
   {
      public TimeSpan ImageTimerDue { get; set; }
      public TimeSpan ImageTimerPeriod { get; set; }

      public required string CameraUrl { get; set; }
      public required string CameraUserName { get; set; }
      public required string CameraUserPassword { get; set; }

      public required string ImageInputPath { get; set; }

      public required string ImageOutputPath { get; set; }

      public required string ModelPath { get; set; }

      public bool UseCuda { get; set; }

      public bool UseTensorrt { get; set; }

      public bool UseRocm { get; set; }

      public bool UseTvm { get; set; }

      public required string TvmSettings { get; set; }

      public int DeviceId { get; set; }
   }
}
