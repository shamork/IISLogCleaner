using System;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace IISLogCleaner
{
    public partial class Service : ServiceBase
    {
        //The root directory to start searching for logs
        private static string _rootLogSearchDirectory = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\logs");
        //The number of days (since last write) that a log can be stale)
        private int _logDaysToKeep = 7;
        //The check interval in minutes
        private static int _intervalInMinutes = 15;
        //If the disk space is below this threshold (in MB) then start deleting the oldest logs first regardless of last write time
        private static long _lowDiskThreshold = 1000;

        private Timer _workTimer;
        private EventLog _eventLog => EventLog;
        
        public Service()
        {
            if (EventLog.SourceExists("IIS Log Cleaner"))
            {
                EventLog.CreateEventSource("IISLogCleaner", "IIS Log Cleaner");
            }
            _eventLog.Source = "IISLogCleaner";

            InitializeComponent();
        }
        
        private string[] args;
        protected override void OnContinue() => base.OnStart(this.args);
        protected override void OnPause() => base.OnStop();
        protected override void OnStart(string[] args)
        {
            this.args = args;
            TimerCallback tcb = DoWork;
            _workTimer = new Timer(tcb, null, 0, _intervalInMinutes*60*1000);
            CheckForTimerChange();

            base.OnStart(args);
            _eventLog.WriteEntry("IIS Log Cleaner Started", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _workTimer.Dispose();
            _eventLog.WriteEntry("IIS Log Cleaner Stopped", EventLogEntryType.Information);
            base.OnStop();
        }

        /// <summary>
        /// Searches the provided directory for log files that meet the config criteria
        /// </summary>
        /// <param name="state">The state provided by the timer callback</param>
        private void DoWork(object state)
        {
            try
            {
                CheckForDirectoryChange();
                CheckForLogDaysChange();
                CheckForLowDiskThresholdChange();
                if (Directory.Exists(_rootLogSearchDirectory))
                {
                    _eventLog.WriteEntry($"cleaning {_rootLogSearchDirectory}", EventLogEntryType.Information);
                    foreach (string path in Directory.GetFileSystemEntries(_rootLogSearchDirectory, "*.log", SearchOption.AllDirectories).OrderBy(File.GetLastAccessTimeUtc))
                    {
                        if (File.Exists(path) && (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(_logDaysToKeep * -1) || LowDiskThresholdCrossed()))
                        {
                            try
                            {
                                File.Delete(path);
                            }
                            catch (Exception ex)
                            {
                                //For some reason we can't delete this. Let's leave it alone 
                                _eventLog.WriteEntry("Error deleting log file: " + path + ". ex:" + ex, EventLogEntryType.Error);
                            }
                        }
                    }
                }
                else
                {
                    _eventLog.WriteEntry($"{_rootLogSearchDirectory} not exists, nothing to do", EventLogEntryType.Warning);
                }

                CheckForTimerChange();
            }
            catch (Exception ex2)
            {
                _eventLog.WriteEntry("Error DoWork. " + " ex:" + ex2, EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Check the config file for a timer settings change
        /// </summary>
        private void CheckForTimerChange()
        {
            int tmpMinutes;
            try
            {
                tmpMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["CheckIntervalMinutes"]);
            }
            catch
            {
                tmpMinutes = 15;
            }

            if (tmpMinutes != _intervalInMinutes)
            {
                LogConfigChangeMsg("CheckIntervalMinutes", _intervalInMinutes, tmpMinutes);
                _intervalInMinutes = tmpMinutes;
                _workTimer.Change(0, _intervalInMinutes*60*1000);
            }
        }

        /// <summary>
        /// Check the config file for a days threshold change
        /// </summary>
        private void CheckForLogDaysChange()
        {
            var raw = _logDaysToKeep;
            try
            {
                _logDaysToKeep = Convert.ToInt32(ConfigurationManager.AppSettings["DaysToKeep"]);
            }
            catch
            {
                _logDaysToKeep = 7;
            }
            LogConfigChangeMsg("DaysToKeep", raw, _logDaysToKeep);
        }

        /// <summary>
        /// Check the config file for a low disk threshold change
        /// </summary>
        private void CheckForLowDiskThresholdChange()
        {
            var raw = _lowDiskThreshold;
            try
            {
                _lowDiskThreshold = Convert.ToInt32(ConfigurationManager.AppSettings["LowDiskThresholdMB"]);
            }
            catch
            {
                _lowDiskThreshold = 1000;
            }
            LogConfigChangeMsg("LowDiskThresholdMB", raw, _lowDiskThreshold);
        }

        /// <summary>
        /// Check the config file for a directory config change
        /// </summary>
        private void CheckForDirectoryChange()
        {
            var raw = _rootLogSearchDirectory;
            try
            {
                _rootLogSearchDirectory = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["RootLogSearchDirectory"]);
            }
            catch
            {
                _rootLogSearchDirectory = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\logs");
            }
            LogConfigChangeMsg("RootLogSearchDirectory", raw, _rootLogSearchDirectory);
        }

        private void LogConfigChangeMsg<T>(string configName,T rawValue,T newValue)
        {
            if (!Equals(rawValue,newValue))
            {
                _eventLog.WriteEntry($"{configName} Change From {rawValue} To {newValue}", EventLogEntryType.Information);
            }
        }

        /// <summary>
        /// Check the log disk for available space exceeding the threshold
        /// </summary>
        /// <returns></returns>
        private bool LowDiskThresholdCrossed()
        {
            var di = new DirectoryInfo(_rootLogSearchDirectory);
            
            long diskSpaceInMB=0;
            try
            {
                var disks = DriveInfo.GetDrives();
                diskSpaceInMB = (disks.First(x => di.Root.Name == x.Name).AvailableFreeSpace) / 1024 / 1024;
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry(
                    $"LowDiskThreshold Check Failed Root:{di.Root.Name} raw path:{_rootLogSearchDirectory} path:{di.FullName} ex:{ex}",
                    EventLogEntryType.Error);
            }

            return diskSpaceInMB < _lowDiskThreshold;
        }
    }
}
