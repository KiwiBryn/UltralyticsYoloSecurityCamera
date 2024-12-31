//---------------------------------------------------------------------------------
// Copyright (c) March 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
using System;

namespace devMobile.IoT.Ultralytics.YoloDotNetRtspCamera.NagerVideoStream.Model
{
	public class ApplicationSettings
	{
		public string RtspCameraUrl { get; set; }

      public string ImageFilepathLocal { get; set; }

      public required string ModelPath { get; set; }
      public required bool CUDA { get; set; } = false;
      public required int GPUId { get; set; } = 0;
      public required bool PrimeGPU { get; set; } = false;
   }
}
