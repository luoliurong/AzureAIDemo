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

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : System.Windows.Window
	{
		#region private fields

		private AppMode _mode;
		private FaceAPI.FaceServiceClient _faceClient = null;
		private VisionAPI.VisionServiceClient _visionClient = null;
		private readonly FrameGrabber<LiveCameraResult> _grabber = null;
		private static readonly ImageEncodingParam[] s_jpegParams = {
			new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
		};
		private readonly CascadeClassifier _localFaceDetector = new CascadeClassifier();
		private bool _fuseClientRemoteResults;
		private LiveCameraResult _latestResultsToDisplay = null;
		private DateTime _startTime;
		private readonly string FACE_API_KEY = SolutionConstant.FaceAPIKey;
		private readonly string FACE_API_ROOT = SolutionConstant.FaceAPIRoot;
		private readonly string VISION_API_KEY = SolutionConstant.VisionAPIKey;
		private readonly string VISION_API_ROOT = SolutionConstant.VisionAPIRoot;

		private readonly string personGroupId = SolutionConstant.personGroupId;
		private ConcurrentDictionary<string, Guid> PersonIdDic = new ConcurrentDictionary<string, Guid>();
		//we have three friends in "myfriends" group
		private readonly string ArasPath = SolutionConstant.ArasData;
		//private static readonly string BillsPath = SolutionConstant.BillsData;
		//my personal image folder
		private readonly string RonsPath = SolutionConstant.RonsData;
		//image for testing, find out my friends in the test face image
		private readonly string ArasPicPath = SolutionConstant.ArasPic;
		private readonly string RonsPicPath = SolutionConstant.RonsPic;

		#endregion

		public MainWindow()
		{
			InitializeComponent();
			
			// Create API clients. 
			_faceClient = new FaceAPI.FaceServiceClient(FACE_API_KEY, FACE_API_ROOT);
			_visionClient = new VisionAPI.VisionServiceClient(VISION_API_KEY, VISION_API_ROOT);

			// Create grabber. 
			_grabber = new FrameGrabber<LiveCameraResult>();

			// Set up a listener for when the client receives a new frame.
			_grabber.NewFrameProvided += (s, e) =>
			{
				// The callback may occur on a different thread, so we must use the
				// MainWindow.Dispatcher when manipulating the UI. 
				this.Dispatcher.BeginInvoke((Action)(() =>
				{
					// Display the image in the left pane.
					LeftImage.Source = e.Frame.Image.ToBitmapSource();

					// If we're fusing client-side face detection with remote analysis, show the
					// new frame now with the most recent analysis available. 
					if (_fuseClientRemoteResults)
					{
						//RightImage.Source = VisualizeResult(e.Frame);
					}
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
						_latestResultsToDisplay = e.Analysis;

						// Display the image and visualization in the right pane. 
						if (!_fuseClientRemoteResults)
						{
							//RightImage.Source = VisualizeResult(e.Frame);
						}
					}
				}));
			};

			// Create local face detector.
			_localFaceDetector.Load("Data/haarcascade_frontalface_alt2.xml");
		}

		#region private methods

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

		private void ModeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Disable "most-recent" results display. 
			_fuseClientRemoteResults = false;

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
		}

		private BitmapSource VisualizeResult(VideoFrame frame)
		{
			// Draw any results on top of the image. 
			BitmapSource visImage = frame.Image.ToBitmapSource();

			var result = _latestResultsToDisplay;

			if (result != null)
			{
				// See if we have local face detections for this image.
				var clientFaces = (OpenCvSharp.Rect[])frame.UserData;
				if (clientFaces != null && result.Faces != null)
				{
					// If so, then the analysis results might be from an older frame. We need to match
					// the client-side face detections (computed on this frame) with the analysis
					// results (computed on the older frame) that we want to display. 
					MatchAndReplaceFaceRectangles(result.Faces, clientFaces);
				}

				visImage = Visualization.DrawFaces(visImage, result.Faces, result.EmotionScores, result.CelebrityNames);
				visImage = Visualization.DrawTags(visImage, result.Tags);
			}

			return visImage;
		}

		private void MatchAndReplaceFaceRectangles(FaceAPI.Contract.Face[] faces, OpenCvSharp.Rect[] clientRects)
		{
			// Use a simple heuristic for matching the client-side faces to the faces in the
			// results. Just sort both lists left-to-right, and assume a 1:1 correspondence. 

			// Sort the faces left-to-right.
			var sortedResultFaces = faces
				.OrderBy(f => f.FaceRectangle.Left + 0.5 * f.FaceRectangle.Width)
				.ToArray();

			// Sort the clientRects left-to-right.
			var sortedClientRects = clientRects
				.OrderBy(r => r.Left + 0.5 * r.Width)
				.ToArray();

			// Assume that the sorted lists now corrrespond directly. We can simply update the
			// FaceRectangles in sortedResultFaces, because they refer to the same underlying
			// objects as the input "faces" array. 
			for (int i = 0; i < Math.Min(faces.Length, clientRects.Length); i++)
			{
				// convert from OpenCvSharp rectangles
				OpenCvSharp.Rect r = sortedClientRects[i];
				sortedResultFaces[i].FaceRectangle = new FaceAPI.Contract.FaceRectangle { Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height };
			}
		}
		
		#endregion

		private async Task<LiveCameraResult> FacesAnalysisFunction(VideoFrame frame)
		{
			FaceAPI.Contract.Face[] faces = null;

			// Encode image. 
			var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
			// Submit image to API. 
			var attrs = new List<FaceAPI.FaceAttributeType> {
				FaceAPI.FaceAttributeType.Age,
				FaceAPI.FaceAttributeType.Gender,
				FaceAPI.FaceAttributeType.HeadPose
			};

			var detectTask = _faceClient.DetectAsync(jpg, returnFaceAttributes: attrs);

			await detectTask.ContinueWith(async (facesDetected) => {

				var faceIds = facesDetected.Result.Select(face => face.FaceId).ToArray();
				faces = facesDetected.Result;
				var identifyRes = await _faceClient.IdentifyAsync(SolutionConstant.personGroupId, faceIds);
				foreach (var identifyResult in identifyRes)
				{
					if (identifyResult.Candidates.Length == 0)
					{
					}
					else
					{
						// Get top 1 among all candidates returned
						var candidateId = identifyResult.Candidates[0].PersonId;
						var person = await _faceClient.GetPersonAsync(SolutionConstant.personGroupId, candidateId);
						var result = $"{identifyResult.FaceId} is identified as '{person.Name}' in {SolutionConstant.personGroupId} person group!";

						await this.Dispatcher.BeginInvoke((Action)(() =>
						{
							ResultList.Items.Add(result);
						}));
					}
				}
			});
			// Count the API call. 
			//Properties.Settings.Default.FaceAPICallCount++;
			// Output. 
			return new LiveCameraResult() { Faces = faces };
		}

		private async Task<LiveCameraResult> EmotionAnalysisFunction(VideoFrame frame)
		{
			// Encode image. 
			var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
			// Submit image to API. 
			FaceAPI.Contract.Face[] faces = null;

			// See if we have local face detections for this image.
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
				// Local face detection found no faces; don't call Cognitive Services.
				faces = new FaceAPI.Contract.Face[0];
			}

			if (faces.Any())
			{
				await this.Dispatcher.BeginInvoke((Action)(() =>
				{
					foreach (var face in faces)
					{
						var bestEmotion = face.FaceAttributes.Emotion.ToRankedList().Select(kv => new Tuple<string, float>(kv.Key, kv.Value)).First();
						var displayText = string.Format("{0}: {1:N1}", bestEmotion.Item1, bestEmotion.Item2);
						ResultList.Items.Add($"Emotion '{displayText}' is detected on face {face.FaceId}.");
					}
				}));
			}

			// Output. 
			return new LiveCameraResult
			{
				Faces = faces.Select(e => CreateFace(e.FaceRectangle)).ToArray(),
				// Extract emotion scores from results. 
				EmotionScores = faces.Select(e => e.FaceAttributes.Emotion).ToArray()
			};
		}

		private async Task<LiveCameraResult> TextDetectAnalyzeFunction(VideoFrame frame)
		{
			// Encode image. 
			var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
			var detectResult = await _visionClient.RecognizeTextAsync(jpg, "unk");

			if (detectResult.Regions.Any())
			{
				foreach (var lr in detectResult.Regions)
				{
					if (lr.Lines.Any())
					{
						StringBuilder builder = new StringBuilder();
						foreach (var line in lr.Lines)
						{
							builder.AppendLine(string.Join(" ", line.Words.Select(w=>w.Text).ToArray()));
						}

						this.Dispatcher.Invoke(() => {
							ResultList.Items.Add($"Text detected: {builder.ToString()}");
						});
					}
				}
			}

			return default(LiveCameraResult);
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
	}
}
