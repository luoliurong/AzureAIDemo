using System;

public static class SolutionConstant
{
	//ms account: 8613816449273
	//hotmail: ronluocellphone@hotmail.com
	//55607767@qq.com/Welcome101
	public static string personGroupId = "myfriends3";
	public static string FaceAPIKey = "e52dfcbfb9f34b66a83499caa8b73847"; //key2: a856f4bbaffc40e5b993f75ec1760d16
	public static string FaceAPIRoot = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0";

	//bkp key2: 62bea676dc764db7b2e35a7bf6072264
	public static string VisionAPIKey = "52daccf50a634b35893aac7dfdf15701";
	//bkp: https://westcentralus.api.cognitive.microsoft.com/vision/v2.0
	public static string VisionAPIRoot = "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0";

	#region data for training and test

	//data to train face api 'myfriend' person group
	public static string ArasData = @"C:\Users\luoliurr\Source\Repos\Cognitive-Face-Windows\Data\PersonGroup\Family2-Daughter";
	//public static string BillsData = @"C:\Users\luoliurr\Source\Repos\Cognitive-Face-Windows\Data\PersonGroup\Family1-Dad";
	public static string RonsData = @"C:\Users\luoliurr\Source\Repos\Cognitive-Face-Windows\Data\PersonGroup\Family2-Dad";

	//data to test the trained pictures
	public static string ArasPic = @"C:\Users\luoliurr\Source\Repos\Cognitive-Face-Windows\Data\ara_test3.jpg";
	public static string RonsPic = @"C:\Users\luoliurr\Source\Repos\Cognitive-Face-Windows\Data\myPic0.jpg";

	#endregion
}
