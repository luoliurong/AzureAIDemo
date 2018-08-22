using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCapturer
{
	public class LiveCameraResult
	{
		public Microsoft.ProjectOxford.Face.Contract.Face[] Faces { get; set; } = null;
		public Microsoft.ProjectOxford.Common.Contract.EmotionScores[] EmotionScores { get; set; } = null;
		public string[] CelebrityNames { get; set; } = null;
		public Microsoft.ProjectOxford.Vision.Contract.Tag[] Tags { get; set; } = null;
	}

	public class LiveAnalyzeResult
	{
		//for text detection result
		public Microsoft.ProjectOxford.Vision.Contract.Region[] Regions { get; set; } = null;

		//for face identification result
		public string[] FaceIdentifyResult { get; set; } = null;

		//for emotion detection result
		public Microsoft.ProjectOxford.Face.Contract.Face[] EmotionFaces { get; set; } = null;
	}
}
