using System;

public static class SolutionConstant
{
	//ms account: 8613816449273
	//hotmail: ronluocellphone@hotmail.com

	public static string personGroupId = "myfriends8";
	public static string FaceAPIKey = "51fa90966e574c9d8c01e2f11f44aa9e"; //key2: 340b676c55c94faba6c676479eb4bfb7
	public static string FaceAPIRoot = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0";

	//bkp key2: 3983bce831a94b4b8d63e67e74e4de02
	public static string VisionAPIKey = "b042077b8541420bab1cba7b72d0e3ea";
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
