// Copyright (c) December 2024, devMobile Software
//
// https://www.gnu.org/licenses/#AGPL
//
// Thanks https://github.com/nager/Nager.VideoStream
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.RTSPCameraSharpRTSP.Model
{
   public class ApplicationSettings
   {
      public string RtspCameraUrl { get; set; } = "";

      public string ImageFilepathLocal { get; set; } = "Images";

      public string CameraUserName { get; set; } = "";

      public string CameraPassword { get; set; } = "";
   }
}
