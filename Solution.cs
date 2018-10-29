using System;

public static class SolutionConstant
{
	//ms account: 8613816449273
	//hotmail: ronluocellphone@hotmail.com
	//55607767@qq.com/Welcome101

	//email:luoliurong163@163.com/Welcome101
	public static string personGroupId = "myfriends";
	public static string FaceAPIKey = "a3bd85c93c464e3d814aacdcf6037c53"; //key2: 530cad178120487484d8efa091ca798c
	public static string FaceAPIRoot = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0";

	//bkp key2: c586a2ac471b4b4faef92e60a3d61558
	public static string VisionAPIKey = "c8573ae036b04eda92aba49fbb2f1149";
	//bkp: https://westcentralus.api.cognitive.microsoft.com/vision/v2.0
	public static string VisionAPIRoot = "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0";

	#region data for training and test

	//data to train face api 'myfriend' person group
	public static string ArasData = @"C:\Users\luoliurr\Source\Repos\AzureAIDemo\TestImages\PersonGroup\Family2-Daughter";
	public static string RonsData = @"C:\Users\luoliurr\Source\Repos\AzureAIDemo\TestImages\PersonGroup\Family2-Dad";

	//data to test the trained pictures
	public static string ArasPic = @"C:\Users\luoliurr\Source\Repos\AzureAIDemo\TestImages\ara_test3.jpg";
	public static string RonsPic = @"C:\Users\luoliurr\Source\Repos\AzureAIDemo\TestImages\myPic0.jpg";

	#endregion
}
