using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MedianStats
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		MainWindow mainWindow;

		void App_Startup(object sender, StartupEventArgs e)
		{
			// Put the exception-handling in front of everything to be sure we catch everything we can.
			// Custom exception-handler to log them.
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
			currentDomain.FirstChanceException += FirstChanceExceptionHandler;

			// Init log before we start anything so it will get logged.
			ConfigureNLog();

			mainWindow = new MainWindow();
		}

		void ConfigureNLog()
		{
			LogManager.ThrowExceptions = true;

			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.DebuggerTarget("logconsole");
			//var logMethod = new NLog.Targets.MethodCallTarget("logMethod", window.ProcessLog);
			var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "MS_logfile.txt" };

			// Rules for mapping loggers to targets            
			config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
			//config.AddRule(LogLevel.Debug, LogLevel.Fatal, logMethod);
			config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

			// Apply config           
			LogManager.Configuration = config;
		}

		private void FirstChanceExceptionHandler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs args)
		{
			Exception e = args.Exception;
			if (e.Message.Contains("MedianStats.XmlSerializers")) {
				// For the initialization of the sounds, an xml-serializer is used. This causes an exception:
				// Die Datei oder Assembly "MedianStats.XmlSerializers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" oder eine Abhängigkeit davon wurde nicht gefunden. Das System kann die angegebene Datei nicht finden.
				// This is normal behavior by microsoft: https://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
				logger.Info("This is an expected exception (XmlSerializers) - FirstChanceException caught: " + e.Message);
				return;
			}
			logger.Fatal("FirstChanceException caught: " + e.Message);
			// Make sure the log is written. Should not be necessary but...
			LogManager.Flush();
		}

		static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
		{
			Exception e = (Exception)args.ExceptionObject;
			logger.Fatal("UnhandledExceptionHandler caught : " + e.Message);
			logger.Fatal("Runtime terminating: {0}", args.IsTerminating);
			// Make sure the log is written. Should not be necessary but...
			LogManager.Flush();
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			Debug.WriteLine("=== Shutdown - after this only cleanup should happen and then exit application ===");
			// == Shutdown NLog ==
			// According to NLog-docu, it is reccomended to call this method when exiting.
			NLog.LogManager.Shutdown(); // Flush and close down internal threads and timers
		}
	}
}
