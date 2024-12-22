//---------------------------------------------------------------------------------
// Copyright (c) November 2023, devMobile Software
// 
// https://www.gnu.org/licenses/#AGPL
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Ultralytics.YoloDotNetCamera.Model
{
   public class ApplicationSettings
   {
      public TimeSpan ImageTimerDue { get; set; }
      public TimeSpan ImageTimerPeriod { get; set; }

      public required string CameraUrl { get; set; }
      public required string CameraUserName { get; set; }
      public required string CameraUserPassword { get; set; }

      public required bool CameraImageSave { get; set; } = false;

      public required string CameraImagePath { get; set; }

      public required bool MarkedUpImageSave { get; set; } = false;

      public required string MarkedUpImagePath { get; set; }

      public required string ModelPath { get; set; }
   }
}
