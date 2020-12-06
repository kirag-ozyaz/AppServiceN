using System;
using System.IO;
using System.Text;

namespace Logger
{
	public static class Log
	{
		public static void Write(LogLevel level, string nameSpace, string className, string methodName, string message, Exception exception = null)
		{
			try
			{
				string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceEchoTelegramLog");
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				string path = Path.Combine(text, string.Format("{0}_{1:dd.MM.yyy}.log", AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
				string text_log = "";
				if (exception != null)
				{
					text_log = string.Format(" >>ERROR: [{0}.{1}] {2}\r\n", exception.TargetSite.DeclaringType, exception.TargetSite.Name, exception.Message);
				}
				string contents = string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}/{2}/{3}:{4}] - {5}{6}\r\n", new object[]
				{
					DateTime.Now,
					nameSpace,
					className,
					methodName,
					level.ToString(),
					message,
					text_log
				});
				lock (Log.obj)
				{
					File.AppendAllText(path, contents, Encoding.GetEncoding("UTF-8"));
				}
			}
			catch (Exception ex)
			{
				
			}
		}

		private static object obj = new object();
	}
}