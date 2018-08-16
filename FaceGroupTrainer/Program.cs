using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceGroupTrainer
{
	class Program
	{
		#region private fields
		private static FaceServiceClient faceServiceClient;
		//we create on group named 'myfriends"
		private static readonly string personGroupId = SolutionConstant.personGroupId;
		private static ConcurrentDictionary<string, Guid> PersonIdDic = new ConcurrentDictionary<string, Guid>();
		//we have three friends in "myfriends" group
		private static readonly string ArasPath = SolutionConstant.ArasData;
		//private static readonly string BillsPath = SolutionConstant.BillsData;
		//my personal image folder
		private static readonly string RonsPath = SolutionConstant.RonsData;
		//image for testing, find out my friends in the test face image
		private static readonly string ArasPicPath = SolutionConstant.ArasPic;
		private static readonly string RonsPicPath = SolutionConstant.RonsPic;
		#endregion

		static void Main(string[] args)
		{
			InitializeServiceClient();

			try
			{
				var task1 = CreatePersonGroupTask();
				DisplayInfo(new List<Task>() { task1 }, $"Creating Person Group {personGroupId} ...");
				task1.Wait();

				var createPersonTaskList = CreatePersonTaskList();
				DisplayInfo(createPersonTaskList, "Waiting to create person 'Ara', and 'Ron'");
				Task.WaitAll(createPersonTaskList.ToArray());

				var uploadImagesTaskList = CreateUploadImageTaskList();
				DisplayInfo(uploadImagesTaskList, "Waiting to upload images for 'Ara' and 'Ron'");
				Task.WaitAll(uploadImagesTaskList.ToArray());

				var trainTask = TrainPersonGroupTask();
				DisplayInfo(new List<Task>() { trainTask }, $"Training Person Group {personGroupId} ...");
				trainTask.Wait();

				var testTask1 = TestImageData(ArasPicPath);
				DisplayInfo(new List<Task> { testTask1 }, $"Test Ara's pictures....");
				var testTask2 = TestImageData(RonsPicPath);
				DisplayInfo(new List<Task> { testTask2 }, $"Test Ron's pictures....");
				Task.WaitAll(new Task[] { testTask1, testTask2 });

				Console.WriteLine("Test completed, press <ENTER> key to continue....");
				Console.ReadLine();
			}
			catch (AggregateException aex)
			{
				Console.WriteLine(aex.Message);
				foreach (var innerEx in aex.InnerExceptions)
				{
					Console.WriteLine(innerEx.Message);
				}
			}
		}

		#region API Calls

		static void InitializeServiceClient()
		{
			//#0. API to initialize FACE API client.
			faceServiceClient = new FaceServiceClient(SolutionConstant.FaceAPIKey, SolutionConstant.FaceAPIRoot);
		}

		static Task CreatePersonGroupTask()
		{
			//#1. API to create a group of persons
			return faceServiceClient.CreatePersonGroupAsync(personGroupId, "My Friends");
		}

		static async Task CreatePersonTask(string personName)
		{
			//#2. API to add persons to a group
			var result = await faceServiceClient.CreatePersonAsync(personGroupId, personName);
			PersonIdDic.TryAdd(personName, result.PersonId);
		}

		static Task UploadImagesForPerson(string dirPath, string personName)
		{
			return Task.Factory.StartNew(async () =>
			{
				foreach (string imagePath in Directory.GetFiles(dirPath, "*.jpg"))
				{
					using (Stream s = File.OpenRead(imagePath))
					{
						Guid personId = Guid.Empty;
						PersonIdDic.TryGetValue(personName, out personId);
						//#3. upload face images of a specific person.
						await faceServiceClient.AddPersonFaceAsync(personGroupId, personId, s);
					}
				}
			});
		}

		static async Task TrainPersonGroupTask()
		{
			//#4. API to train the person groups
			await faceServiceClient.TrainPersonGroupAsync(personGroupId);

			TrainingStatus trainingStatus = null;
			while (true)
			{
				trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

				if (trainingStatus.Status != Status.Running)
				{
					break;
				}

				await Task.Delay(1000);
			}
		}

		static async Task TestImageData(string pictureToTest)
		{
			try
			{
				var idx = pictureToTest.LastIndexOf('\\');
				var picFileName = pictureToTest.Substring(idx + 1);
				Console.WriteLine($"Identifling persons in provided picture <{picFileName}>....");
				using (Stream s = File.OpenRead(pictureToTest))
				{
					//#5. API to detect a customized face image.
					var faces = await faceServiceClient.DetectAsync(s);
					if (!faces.Any())
					{
						Console.WriteLine($"Did not detect any faces in picture {picFileName}");
					}
					else
					{
						var faceIds = faces.Select(face => face.FaceId).ToArray();

						//#6. Identify the result for the customized image.
						var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
						foreach (var identifyResult in results)
						{
							if (identifyResult.Candidates.Length == 0)
							{
								Console.WriteLine($"FaceId {identifyResult.FaceId} is identified as 'Unknown' in picture {picFileName}.");
							}
							else
							{
								// Get top 1 among all candidates returned
								var candidateId = identifyResult.Candidates[0].PersonId;
								var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
								if (person != null && !string.IsNullOrWhiteSpace(person.Name))
								{
									Console.WriteLine($"FaceId {identifyResult.FaceId} is identified as {person.Name} in picture <{picFileName}>");
								}
							}
						}
					}
				}
			}
			catch (FaceAPIException faex)
			{
				throw faex;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		static void DisplayInfo(List<Task> myTasks, string messageToDisplay)
		{
			while (!myTasks.All(t => t.IsCompleted))
			{
				Thread.Sleep(1000);
				Console.WriteLine(messageToDisplay);
			}
		}

		static List<Task> CreatePersonTaskList()
		{
			var taskList = new List<Task>();
			var task1 = CreatePersonTask("Ara");
			var task2 = CreatePersonTask("Ron");
			taskList.Add(task1);
			taskList.Add(task2);

			return taskList;
		}

		static List<Task> CreateUploadImageTaskList()
		{
			var uploadTasks = new List<Task>();
			var upload1Task = UploadImagesForPerson(ArasPath, "Ara");
			var upload2Task = UploadImagesForPerson(RonsPath, "Ron");

			uploadTasks.Add(upload1Task);
			uploadTasks.Add(upload2Task);

			return uploadTasks;
		}

		#endregion
	}
}
