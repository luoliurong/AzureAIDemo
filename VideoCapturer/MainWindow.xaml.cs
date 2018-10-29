using FrameCollector;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Common = Microsoft.ProjectOxford.Common;
using FaceAPI = Microsoft.ProjectOxford.Face;
using VisionAPI = Microsoft.ProjectOxford.Vision;

namespace VideoCapturer
{
	public enum AppMode
	{
		Faces,
		Emotions,
		Text
	}

	public partial class MainWindow : System.Windows.Window
	{
		#region private fields

		private AppMode _mode;
		private FaceAPI.FaceServiceClient _faceClient = null;
		private VisionAPI.VisionServiceClient _visionClient = null;
		private readonly FrameGrabber<LiveAnalyzeResult> _grabber = null;
		private static readonly ImageEncodingParam[] s_jpegParams = {
			new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
		};
		private readonly CascadeClassifier _localFaceDetector = new CascadeClassifier();
		private DateTime _startTime;

		private readonly string FACE_API_KEY = SolutionConstant.FaceAPIKey;
		private readonly string FACE_API_ROOT = SolutionConstant.FaceAPIRoot;
		private readonly string VISION_API_KEY = SolutionConstant.VisionAPIKey;
		private readonly string VISION_API_ROOT = SolutionConstant.VisionAPIRoot;
		private readonly string personGroupId = SolutionConstant.personGroupId;
		private ConcurrentDictionary<string, Guid> PersonIdDic = new ConcurrentDictionary<string, Guid>();

		//image for training in congnitive face api
		private readonly string ArasPath = SolutionConstant.ArasData;
		private readonly string RonsPath = SolutionConstant.RonsData;

		//image for testing, find out my friends in the test face image
		private readonly string ArasPicPath = SolutionConstant.ArasPic;
		private readonly string RonsPicPath = SolutionConstant.RonsPic;

		#endregion

		public MainWindow()
		{
			_grabber = new FrameGrabber<LiveAnalyzeResult>();
			InitializeComponent();
			InitializeClientAPIs();
		}

		private void InitializeClientAPIs()
		{
			// Create API clients. 
			_faceClient = new FaceAPI.FaceServiceClient(FACE_API_KEY, FACE_API_ROOT);
			_visionClient = new VisionAPI.VisionServiceClient(VISION_API_KEY, VISION_API_ROOT);

			_grabber.NewFrameProvided += (s, e) =>
			{
				this.Dispatcher.BeginInvoke((Action)(() =>
				{
					// Display the image in the left pane captured from camera.
					LeftImage.Source = e.Frame.Image.ToBitmapSource();
				}));

				// See if auto-stop should be triggered. 
				if (Properties.Settings.Default.AutoStopEnabled && (DateTime.Now - _startTime) > Properties.Settings.Default.AutoStopTime)
				{
					_grabber.StopProcessingAsync();
				}
			};

			// Set up a listener for when the client receives a new result from an API call. 
			_grabber.NewResultAvailable += (s, e) =>
			{
				this.Dispatcher.BeginInvoke((Action)(() =>
				{
					if (e.TimedOut)
					{
						MessageArea.Text = "API call timed out.";
					}
					else if (e.Exception != null)
					{
						string apiName = "";
						string message = e.Exception.Message;
						var faceEx = e.Exception as FaceAPI.FaceAPIException;
						var emotionEx = e.Exception as Common.ClientException;
						var visionEx = e.Exception as VisionAPI.ClientException;
						if (faceEx != null)
						{
							apiName = "Face";
							message = faceEx.ErrorMessage;
						}
						else if (emotionEx != null)
						{
							apiName = "Emotion";
							message = emotionEx.Error.Message;
						}
						else if (visionEx != null)
						{
							apiName = "Computer Vision";
							message = visionEx.Error.Message;
						}
						MessageArea.Text = string.Format("{0} API call failed on frame {1}. Exception: {2}", apiName, e.Frame.Metadata.Index, message);
					}
					else
					{
						if (_mode == AppMode.Text)
						{
							if (e.AnalysisResult != null)
							{
								foreach (var lr in e.AnalysisResult.Regions)
								{
									if (lr.Lines.Any())
									{
										StringBuilder builder = new StringBuilder();
										foreach (var line in lr.Lines)
										{
											builder.AppendLine(string.Join(" ", line.Words.Select(w => w.Text).ToArray()));
										}

										this.Dispatcher.Invoke(() =>
										{
											ResultList.Items.Add($"Text detected: {builder.ToString()}");
										});
									}
								}
							}
						}
						else if (_mode == AppMode.Emotions)
						{
							if (e.AnalysisResult != null)
							{
								var faces = e.AnalysisResult.EmotionFaces;
								this.Dispatcher.Invoke(() =>
								{
									foreach (var face in faces)
									{
										var bestEmotion = face.FaceAttributes.Emotion.ToRankedList().Select(kv => new Tuple<string, float>(kv.Key, kv.Value)).First();
										var displayText = string.Format("{0}: {1:N1}", bestEmotion.Item1, bestEmotion.Item2);
										ResultList.Items.Add($"Emotion '{displayText}' is detected on face {face.FaceId}.");
									}
								});
							}
						}
						else if (_mode == AppMode.Faces)
						{
							if (e.AnalysisResult != null)
							{
								var result = e.AnalysisResult.FaceIdentifyResult;
								this.Dispatcher.Invoke((Action)(() =>
								{
									foreach (var sItem in result)
									{
										ResultList.Items.Add(sItem);
									}
								}));
							}
						}
					}
				}));
			};

			// Create local face detector.
			_localFaceDetector.Load("Data/haarcascade_frontalface_alt2.xml");
		}

		#region 1-prepare person group for identification, including (create person group, add persons into the group, upload images and then train the group)

		private async void BtnCreate_Click(object sender, RoutedEventArgs e)
		{
			var progress = new Progress<string>(msg => {
				this.Dispatcher.Invoke(() =>
				{
					this.ResultList.Items.Add(msg);
				});
			});

			await DoBackgroudWork(progress);

			this.ResultList.Items.Add("Preparation work tasks completed!");
		}

		private async Task DoBackgroudWork(IProgress<string> progress)
		{
			//step1: to create a person group
			progress.Report("Executing task to create person group...");
			await _faceClient.CreatePersonGroupAsync(personGroupId, "My Friends");

			//step2: to add persons into person group
			progress.Report("Executing task to add 'Ara' into person group...");
			var result = await _faceClient.CreatePersonAsync(personGroupId, "Ara");
			PersonIdDic.TryAdd("Ara", result.PersonId);

			progress.Report("Executing task to add 'Ron' into person group...");
			result = await _faceClient.CreatePersonAsync(personGroupId, "Ron");
			PersonIdDic.TryAdd("Ron", result.PersonId);

			//step3: to upload images for persons in group:
			progress.Report("Executing task to upload pictures of 'Ara'...");
			await UploadImagesForPerson(ArasPath, "Ara");

			progress.Report("Executing task to upload pictures of 'Ron'...");
			await UploadImagesForPerson(RonsPath, "Ron");

			//step4: to train the person group
			progress.Report(string.Format("Executing task to train the person group {0}...", personGroupId));
			await TrainPersonGroupTask();
		}

		private async Task UploadImagesForPerson(string dirPath, string personName)
		{
			Guid personId = Guid.Empty;
			PersonIdDic.TryGetValue(personName, out personId);
			foreach (string imagePath in Directory.GetFiles(dirPath, "*.jpg"))
			{
				try
				{
					using (Stream s = File.OpenRead(imagePath))
					{
						//#3. upload face images of a specific person.
						await _faceClient.AddPersonFaceAsync(personGroupId, personId, s);
					}
				}
				catch
				{
					continue;
				}
			}
		}

		private async Task TrainPersonGroupTask()
		{
			//#4. API to train the person groups
			await _faceClient.TrainPersonGroupAsync(personGroupId);

			FaceAPI.Contract.TrainingStatus trainingStatus = null;
			while (true)
			{
				trainingStatus = await _faceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

				if (trainingStatus.Status != FaceAPI.Contract.Status.Running)
				{
					break;
				}

				await Task.Delay(1000);
			}
		}

		#endregion

		private async void StartButton_Click(object sender, RoutedEventArgs e)
		{
			ResultList.Items.Clear();

			if (!CameraList.HasItems)
			{
				MessageArea.Text = "No cameras found; cannot start processing";
				return;
			}

			// How often to analyze. 
			_grabber.TriggerAnalysisOnInterval(Properties.Settings.Default.AnalysisInterval);

			// Reset message. 
			MessageArea.Text = "";

			// Record start time, for auto-stop
			_startTime = DateTime.Now;

			await _grabber.StartProcessingCameraAsync(CameraList.SelectedIndex);
		}

		private async void StopButton_Click(object sender, RoutedEventArgs e)
		{
			await _grabber.StopProcessingAsync();
		}

		private void CameraList_Loaded(object sender, RoutedEventArgs e)
		{
			int numCameras = _grabber.GetNumCameras();

			if (numCameras == 0)
			{
				MessageArea.Text = "No cameras found!";
			}

			var comboBox = sender as ComboBox;
			comboBox.ItemsSource = Enumerable.Range(0, numCameras).Select(i => string.Format("Camera {0}", i + 1));
			comboBox.SelectedIndex = 0;
		}

		private void ModeList_Loaded(object sender, RoutedEventArgs e)
		{
			var modes = (AppMode[])Enum.GetValues(typeof(AppMode));
			var comboBox = sender as ComboBox;
			comboBox.ItemsSource = modes.Select(m => m.ToString());
			comboBox.SelectedIndex = 0;
		}

		private async void ModeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await _grabber.StopProcessingAsync();
			var comboBox = sender as ComboBox;
			var modes = (AppMode[])Enum.GetValues(typeof(AppMode));
			_mode = modes[comboBox.SelectedIndex];
			switch (_mode)
			{
				case AppMode.Faces:
					_grabber.AnalysisFunction = FacesAnalysisFunction;
					break;
				case AppMode.Emotions:
					_grabber.AnalysisFunction = EmotionAnalysisFunction;
					break;
				case AppMode.Text:
					_grabber.AnalysisFunction = TextDetectAnalyzeFunction;
					break;
				default:
					_grabber.AnalysisFunction = null;
					break;
			}
			await _grabber.StartProcessingCameraAsync();
		}

		private async Task<LiveAnalyzeResult> FacesAnalysisFunction(VideoFrame frame)
		{
			FaceAPI.Contract.Face[] faces = null;
			var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
			var attrs = new List<FaceAPI.FaceAttributeType> {
				FaceAPI.FaceAttributeType.Age,
				FaceAPI.FaceAttributeType.Gender,
				FaceAPI.FaceAttributeType.HeadPose
			};
			faces = await _faceClient.DetectAsync(jpg, returnFaceAttributes: attrs);
			var resultList = new List<string>();

			var faceIds = faces.Select(face => face.FaceId).ToArray();

			var identifyRes = await _faceClient.IdentifyAsync(SolutionConstant.personGroupId, faceIds);
			foreach (var identifyResult in identifyRes)
			{
				if (identifyResult.Candidates.Length > 0)
				{
					// Get top 1 among all candidates returned, the highest scored candidate
					var candidateId = identifyResult.Candidates[0].PersonId;
					var person = await _faceClient.GetPersonAsync(SolutionConstant.personGroupId, candidateId);
					var result = $"{identifyResult.FaceId} is identified as '{person.Name}' in {SolutionConstant.personGroupId} person group!";

					resultList.Add(result);
				}
			}

			return new LiveAnalyzeResult() { FaceIdentifyResult = resultList.ToArray() };
		}

		private async Task<LiveAnalyzeResult> EmotionAnalysisFunction(VideoFrame frame)
		{
			var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
			FaceAPI.Contract.Face[] faces = null;

			var localFaces = (OpenCvSharp.Rect[])frame.UserData;
			if (localFaces == null || localFaces.Count() > 0)
			{
				// If localFaces is null, we're not performing local face detection.
				// Use Cognigitve Services to do the face detection.
				//Properties.Settings.Default.FaceAPICallCount++;
				faces = await _faceClient.DetectAsync(
					jpg,
					/* returnFaceId= */ false,
					/* returnFaceLandmarks= */ false,
					new FaceAPI.FaceAttributeType[1] { FaceAPI.FaceAttributeType.Emotion });
			}
			else
			{
				faces = new FaceAPI.Contract.Face[0];
			}

			if (faces.Any())
			{
				return new LiveAnalyzeResult()
				{
					EmotionFaces = faces.ToArray()
				};
			}

			return default(LiveAnalyzeResult);
		}

		private async Task<LiveAnalyzeResult> TextDetectAnalyzeFunction(VideoFrame frame)
		{
			// Encode image. 
			var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
			var detectResult = await _visionClient.RecognizeTextAsync(jpg, "unk");

			if (detectResult.Regions.Any())
			{
				return new LiveAnalyzeResult() { Regions = detectResult.Regions };
			}

			return default(LiveAnalyzeResult);
		}

		private FaceAPI.Contract.Face CreateFace(FaceAPI.Contract.FaceRectangle rect)
		{
			return new FaceAPI.Contract.Face
			{
				FaceRectangle = new FaceAPI.Contract.FaceRectangle
				{
					Left = rect.Left,
					Top = rect.Top,
					Width = rect.Width,
					Height = rect.Height
				}
			};
		}
	}
}
