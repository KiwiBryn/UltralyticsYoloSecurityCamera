// Copyright (c) December 2024, devMobile Software
//
// https://www.gnu.org/licenses/#AGPL
//
// Thanks https://github.com/nager/Nager.VideoStream
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Ultralytics.YoloDotNetRtspCamera.NagerVideoStream.Model
{
	public class ApplicationSettings
	{
      public string RtspCameraUrl { get; set; } = "";

      public string FFMpegPath { get; set; } = "ffmpeg.exe";

      public string ImageFilepathLocal { get; set; } = "Images";

      public required bool Inference { get; set; } = false;

      public required string ModelPath { get; set; } = "";

      public required bool MarkUpImages { get; set; } = false;

      public required bool CUDA { get; set; } = false;
      public required int GPUId { get; set; } = 0;
      public required bool PrimeGPU { get; set; } = false;
   }
}
