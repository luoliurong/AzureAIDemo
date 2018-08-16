using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FrameCollector
{
	public static class ConcurrentLogger
	{
		private readonly static SemaphoreSlim s_printMutext = new SemaphoreSlim(1);
		private readonly static BlockingCollection<string> s_messageQueue = new BlockingCollection<string>();

		public static void WriteLine(string message)
		{
			var timestamp = DateTime.Now;
			// Push the message on the queue
			s_messageQueue.Add(timestamp.ToString("o") + ": " + message);
			// Start a new task that will dequeue one message and print it. The tasks will not
			// necessarily run in order, but since each task just takes the oldest message and
			// prints it, the messages will print in order. 
			Task.Run(async () =>
			{
				// Wait to get access to the queue. 
				await s_printMutext.WaitAsync();
				try
				{
					string msg = s_messageQueue.Take();
					Console.WriteLine(msg);
				}
				finally
				{
					s_printMutext.Release();
				}
			});
		}

		public static void WriteToFile(string message)
		{
			var timestamp = DateTime.Now;
			s_messageQueue.Add(timestamp.ToString("o") + ": " + message);

			Task.Run(async () =>
			{
				await s_printMutext.WaitAsync();
				try
				{
					var msg = s_messageQueue.Take();
					using (System.IO.StreamWriter writer = new System.IO.StreamWriter("FrameCollector.log", true, Encoding.UTF8))
					{
						writer.WriteLine(msg);
						writer.WriteLine("===========================================================================");
					}
				}
				finally
				{
					s_printMutext.Release();
				}
			});
		}
	}
}
