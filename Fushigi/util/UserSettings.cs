using Fushigi.gl.Bfres;
using Fushigi.ui;
using Fushigi.ui.widgets;
using Newtonsoft.Json;

namespace Fushigi.util
{
    public static class UserSettings
    {
        public static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Fushigi"
            );
        public static readonly string SettingsFile = Path.Combine(SettingsDir, "UserSettings.json");
        public static readonly int MaxRecents = 10;
        static Settings AppSettings;

        struct Settings
        {
            public string RomFSPath;
            public string RomFSModPath;
            public float BackupFreqMinutes = 10;
            public Dictionary<string, string> ModPaths;
            public List<string> RecentCourses;
            public bool UseGameShaders;
            public bool RenderCustomModels;
            public bool UseAstcTextureCache;
            public bool HideDeletingLinkedActorsPopup;
            public bool UseNewCamera;
            public bool EnableHalfTile;
            public bool EnableTranslation;
            public bool PrivateDRPC;
            public string Theme;
            public int ShaderSettings;
            public bool romfsReload;
            public bool allowRomfsReload;
            public bool fpsCounter = true;
            public bool advancedDebugOptions;
            public bool VSync;

            public Settings()
            {
                BackupFreqMinutes = 10;
                RomFSPath = "";
                ModPaths = [];
                RomFSModPath = "";
                RecentCourses = new List<string>(MaxRecents) { };
                RenderCustomModels = false;
                UseGameShaders = false;
                UseAstcTextureCache = false;
                HideDeletingLinkedActorsPopup = false;
                UseNewCamera = true;
                EnableHalfTile = false;
                EnableTranslation = true;
                Theme = "Theme";
                ShaderSettings = 0;
                romfsReload = false;
                allowRomfsReload = true;
                fpsCounter = true;
                advancedDebugOptions = false;
                VSync = false;
            }
        }

        public static void Load()
        {
            AppSettings = new Settings();
            if (File.Exists(SettingsFile))
            {
                try
                {
                    AppSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFile));
                    AppSettings.RecentCourses = AppSettings.RecentCourses ?? new List<string>();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.GetType}: {e.Message}");
                    Console.WriteLine("Creating new User Settings.");
                }
            }
        }

        public static void Save()
        {
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
            }

            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(AppSettings, Formatting.Indented));
        }

        public static ref bool RefUseGameShaders => ref AppSettings.UseGameShaders;
        public static bool UseGameShaders
        {
            get => AppSettings.UseGameShaders;
            set
            {
                AppSettings.UseGameShaders = value;
                Save();
            }
        }
        public static ref bool RefUseAstcTextureCache => ref AppSettings.UseAstcTextureCache;
        public static bool UseAstcTextureCache
        {
            get => AppSettings.UseAstcTextureCache;
            set
            {
                AppSettings.UseAstcTextureCache = value;
                Save();
            }
        }
        public static ref bool RefHideDeletingLinkedActorsPopup => ref AppSettings.HideDeletingLinkedActorsPopup;
        public static bool HideDeletingLinkedActorsPopup
        {
            get => AppSettings.HideDeletingLinkedActorsPopup;
            set
            {
                AppSettings.HideDeletingLinkedActorsPopup = value;
                Save();
            }
        }
        public static ref bool RefRenderCustomModels => ref AppSettings.RenderCustomModels;
        public static bool RenderCustomModels
        {
            get => AppSettings.RenderCustomModels;
            set
            {
                AppSettings.RenderCustomModels = value;
                Save();
                BfresCache.Clear();
            }
        }
        public static ref string RefRomFSPath => ref AppSettings.RomFSPath;
        public static string RomFSPath
        {
            get => AppSettings.RomFSPath;
            set
            {
                AppSettings.RomFSPath = value;
                Save();
            }
        }
        public static ref string RefModRomFSPath => ref AppSettings.RomFSModPath;
        public static string ModRomFSPath
        {
            get => AppSettings.RomFSModPath;
            set
            {
                AppSettings.RomFSModPath = value;
                Save();
            }
        }
        public static ref bool RefUseNewCamera => ref AppSettings.UseNewCamera;
        public static bool UseNewCamera
        {
            get => AppSettings.UseNewCamera;
            set
            {
                AppSettings.UseNewCamera = value;
                Save();
            }
        }
        public static ref float RefBackupFreqMinutes => ref AppSettings.BackupFreqMinutes;
        public static float BackupFreqMinutes
        {
            get
            {
                if (AppSettings.BackupFreqMinutes < 1)
                    AppSettings.BackupFreqMinutes = 10;
                return AppSettings.BackupFreqMinutes;
            }
            set
            {
                if (value < 1)
                    value = 10;
                AppSettings.BackupFreqMinutes = value;
                Save();
            }
        }
        public static ref bool RefEnableHalfTile => ref AppSettings.EnableHalfTile;
        public static bool EnableHalfTile
        {
            get => AppSettings.EnableHalfTile;
            set
            {
                AppSettings.EnableHalfTile = value;
                Save();
            }
        }
        public static ref bool RefEnableTranslation => ref AppSettings.EnableTranslation;
        public static bool EnableTranslation
        {
            get => AppSettings.EnableTranslation;
            set
            {
                AppSettings.EnableTranslation = value;
                CourseScene.refreshTranslation = true;
                Save();
            }
        }
        public static ref bool RefSetPrivateDRPC => ref AppSettings.PrivateDRPC;
        public static bool PrivateDRPC
        {
            get => AppSettings.PrivateDRPC;
            set
            {
                AppSettings.PrivateDRPC = value;
                Save();
            }
        }
        public static ref bool RefFPSCounter => ref AppSettings.fpsCounter;
        public static bool FPSCounter
        {
            get => AppSettings.fpsCounter;
            set
            {
                AppSettings.fpsCounter = value;
                Save();
            }
        }
        public static ref bool RefAdvancedDebugSettings => ref AppSettings.advancedDebugOptions;
        public static bool AdvancedDebugSettings
        {
            get => AppSettings.advancedDebugOptions;
            set
            {
                AppSettings.advancedDebugOptions = value;
                Save();
            }
        }
        public static ref bool RefVSync => ref AppSettings.VSync;
        public static bool VSync
        {
            get => AppSettings.VSync;
            set
            {
                Program.MainWindow.Window.VSync = value;
                AppSettings.VSync = value;
                Save();
            }
        }
        public static ref string RefTheme => ref AppSettings.Theme;
        public static string Theme
        {
            get => AppSettings.Theme;
            set
            {
                AppSettings.Theme = value;
                Save();
            }
        }
        public static ref int RefShaderSettings => ref AppSettings.ShaderSettings;
        public static int ShaderSettings
        {
            get => AppSettings.ShaderSettings;
            set
            {
                AppSettings.ShaderSettings = value;
                Save();
            }
        }
        public static ref bool RefRomFSReload => ref AppSettings.romfsReload;
        public static bool RomFSReload
        {
            get => AppSettings.romfsReload;
            set => AppSettings.romfsReload = value;
        }
        public static ref bool RefAllowRomFSReload => ref AppSettings.allowRomfsReload;
        public static bool AllowRomFSReload
        {
            get => AppSettings.allowRomfsReload;
            set
            {
                AppSettings.allowRomfsReload = value;
                Save();
            }
        }

        public static void AppendModPath(string modname, string path)
        {
            AppSettings.ModPaths.Add(modname, path);
        }

        public static void AppendRecentCourse(string courseName)
        {
            // please let me know if this isn't a good implementation
            if (AppSettings.RecentCourses.Count == MaxRecents)
            {
                // since we only store the last 10, we push our array once to the left
                // then our new entry is appended on the 9th index
                var oldArray = AppSettings.RecentCourses.ToArray();
                var newArray = new string?[oldArray.Length];
                Array.Copy(oldArray, 1, newArray, 0, oldArray.Length - 1);

                AppSettings.RecentCourses = [.. newArray];
                // put our brand new path at 9
                AppSettings.RecentCourses[MaxRecents - 1] = courseName;
            }
            else
            {
                AppSettings.RecentCourses.Add(courseName);
            }
        }

        public static string? GetLatestCourse()
        {
            //int size = AppSettings.RecentCourses.Count;
            if (AppSettings.RecentCourses.Count == 0)
            {
                return null;
            }

            return AppSettings.RecentCourses.Last();
        }
    }
}
