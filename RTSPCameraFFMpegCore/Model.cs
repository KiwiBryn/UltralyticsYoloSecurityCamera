//---------------------------------------------------------------------------------
// Copyright (c) January 2025, devMobile Software
//
// https://mit-license.org/
//
// Thanks https://github.com/rosenbjerg/FFMpegCore/
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.RTSPCameraFFMpegCore.Model
{
   public class ApplicationSettings
   {
      public string RtspCameraUrl { get; set; } = "";

      public string ImageFilepathLocal { get; set; } = "Images";

      public string CameraUserName { get; set; } = "";

      public string CameraPassword { get; set; } = "";
   }
}
