using DiscordRPC;
using DiscordRPC.Logging;
using Fushigi.ui.widgets;
using Fushigi.util;

namespace Fushigi
{
    public static class DRPC
    {
        const string mClientID = "1227725734429196298";

        static DiscordRpcClient? mClient;
        static readonly DateTime mStartTime = DateTime.Now.ToUniversalTime();

        public static void Initialize()
        {
            mClient = new DiscordRpcClient(mClientID)
            {
                Logger = new RPCLogger()
            };

            AppDomain.CurrentDomain.ProcessExit += Dispose;

            mClient.OnReady += (sender, e) =>
            {
                Logger.Logger.LogMessage("DiscordRPC", "Ready");
            };

            mClient.Initialize();
        }

        private static void Dispose(object? sender, EventArgs e)
        {
            mClient?.Dispose();
        }

        internal static void UpdateDRPC(CourseScene cs)
        {
            string sCourseID = cs.course.GetName().Split("_")[0].Replace("Course", "");
            if (int.TryParse(sCourseID, out int courseID))
            {
                if (RomFS.CourseNames.TryGetValue(courseID, out string? courseName))
                {
                    RomFS.CourseWorlds.TryGetValue(courseID, out int worldID);
                    DRPC.SetEditingCourse(cs.selectedArea.GetName(), courseName, worldID);
                    Program.MainWindow.SetWindowIcon(worldID);
                }
                else
                    Logger.Logger.LogWarning("CourseScene", $"Failed to get course name for {cs.course.GetName()}");

                return;
            }

            Logger.Logger.LogWarning("CourseScene", $"Failed to get course ID for {cs.course.GetName()}");
        }


        static void SetPresence(string details, string state, string imageKey)
        {
            if (mClient == null)
            {
                Logger.Logger.LogWarning("DiscordRPC", "Tried to set presence with a null client");
                return;
            }

            mClient.SetPresence(new RichPresence()
            {
                Details = details,
                State = state,
                Assets = new Assets()
                {
                    LargeImageKey = imageKey,
                    LargeImageText = $"Fushigi {Program.Version}"
                },
                Timestamps = new Timestamps()
                {
                    Start = mStartTime
                }
            });
        }

        public static void SetEditingCourse(string courseID, string courseName, int worldNumber)
        {
            if (UserSettings.GetPrivateDRPC())
                SetPresence($"Editing a Course", null, "icon" + 1);
            else
                SetPresence($"Editing {courseID}", courseName, "icon" + worldNumber);
        }

        class RPCLogger : ILogger
        {
            LogLevel mLogLevel;

            public LogLevel Level { get => mLogLevel; set => mLogLevel = value; }

            public RPCLogger()
            {
                mLogLevel = LogLevel.Warning;
            }

            public void Error(string message, params object[] args)
            {
                Logger.Logger.LogError("DiscordRPC", message);
            }

            public void Info(string message, params object[] args)
            {
                //Logger.Logger.LogMessage("DiscordRPC", message);
            }

            public void Trace(string message, params object[] args)
            {
                //Logger.Logger.LogMessage("DiscordRPC", message);
            }

            public void Warning(string message, params object[] args)
            {
                Logger.Logger.LogWarning("DiscordRPC", message);
            }
        }
    }
}
