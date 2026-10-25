using Fushigi.course;
using Fushigi.rstb;
using Fushigi.util;

public class CourseSave
{
    public static bool saveStatus = true;
    public static double backupTime = 0;
    public static void Save(bool backup, Course course)
    {
        //RomFS.courseNamesMSBT.Messages = RomFS.courseNames;
        //RomFS.courseNamesMSBT.Save(Path.Combine(UserSettings.GetModRomFSPath(), "Mals", "GameMsg", "Name_CourseRemoveLineFeed.msbt"));

        saveStatus = true;
        string savePath = UserSettings.GetModRomFSPath();
        DateTime now = DateTime.Now;

        if (backup)
        {
            savePath = Path.Combine(UserSettings.SettingsDir, "backups");
            savePath = Path.Combine(savePath, $"{now.Year}-{now.Month}-{now.Day}_{now.Hour}-{now.Minute}-{now.Second}");
        }

        var rstbPath = Path.Combine(UserSettings.GetRomFSPath(), "System", "Resource");

        string[] sizeTables = Directory.GetFiles(rstbPath, "*.zs");

        foreach (string path in sizeTables)
        {

            RSTB resource_table = new RSTB();
            resource_table.Load(Path.GetFileName(path));

            List<string> pathsToWriteTo;

            //Save the Course file
            Console.WriteLine($"{(backup ? "Backing up" : "Saving")} course {course.GetName()}...");
            course.SaveCourse(resource_table, Path.Combine(savePath, "BancMapUnit"));


            //Save the CourseInfo file
            if (!Course.IsWorldMap)
            {
                Console.WriteLine($"{(backup ? "Backing up" : "Saving")} course info for {course.GetName()}...");
                course.mCourseInfo.Save(resource_table, Path.Combine(savePath, "Stage", "CourseInfo"), course.GetName());
                //Save the MapAnalysisInfo file
                Console.WriteLine($"{(backup ? "Backing up" : "Saving")} map analysis info for {course.GetName()}...");
                course.mMapAnalysisInfo.Save(resource_table, Path.Combine(savePath, "Stage", "MapAnalysisInfo"), course.GetName());

            }

            //Save the StageLoadInfo file
            Console.WriteLine($"{(backup ? "Backing up" : "Saving")} stage load info for {course.GetName()}...");
            course.mStageLoadInfo.Save(resource_table, Path.Combine(savePath, "Stage", "StageLoadInfo"), course.GetName());

            foreach (var area in course.GetAreas())
            {
                Console.WriteLine($"{(backup ? "Backing up" : "Saving")} area {area.GetName()}...");
                Console.WriteLine($"{(backup ? "Backing up" : "Saving")} area parameters for {area.GetName()}...");

                var name = area.mAreaParams.EnvPaletteSetting.InitPaletteBaseName;

                //if (hasOpened)
                //    envPaletteWindow.SavePalette(resource_table, Path.Combine(savePath, "Gyml", "Gfx", "EnvPaletteParam"));

                area.Save(resource_table, Path.Combine(savePath, "BancMapUnit"), false);
                area.mAreaParams.Save(resource_table, Path.Combine(savePath, "Stage", "AreaParam"), area.mAreaName, false);

                if (!Course.IsWorldMap)
                    area.SaveStageParam(resource_table, Path.Combine(savePath));
            }
            resource_table.Save(savePath);
        }
    }

    public static bool HasUnsavedChanges()
    {
        if (!saveStatus)
            return true;
        else
            return false;
    }

    bool EnsureFileIsWritable(string path)
    {
        if (!File.Exists(path))
            return true;
        try
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                return fs.CanWrite;
            }
        }
        catch (IOException e)
        {
            return false;
        }
    }
}
