using Microsoft.ProjectOxford.Vision.Contract;

namespace VideoCapturer
{
	public class LiveAnalyzeResult
	{
		//for text detection result
		public Region[] Regions { get; set; } = null;

		//for face identification result
		public string[] FaceIdentifyResult { get; set; } = null;

		//for emotion detection result
		public Microsoft.ProjectOxford.Face.Contract.Face[] EmotionFaces { get; set; } = null;
	}
}
