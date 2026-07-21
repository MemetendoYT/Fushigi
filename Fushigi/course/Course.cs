using Fasterflect;
using Fushigi.Byml;
using Fushigi.env;
using Fushigi.gl;
using Fushigi.Logger;
using Fushigi.rstb;
using Fushigi.util;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Fushigi.course
{
    public class Course
    {
        public Course(string courseName)
        {
            mCourseName = courseName;

            if (!IsWorldMap)
            {
                mCourseInfo = new CourseInfo(mCourseName, IsWorldMap);
                mMapAnalysisInfo = new MapAnalysisInfo(mCourseName);
            }
            else
            {
                mWorldInfo = new WorldMapInfo(mCourseName);
            }
            mStageLoadInfo = new StageLoadInfo(courseName);
            mAreas = [];
            LoadFromRomFS();
        }
        public string GetName()
        {
            return mCourseName;
        }

        public static void SetIsWorldMap(bool value)
        {
            IsWorldMap = value;
        }
        public static bool GetIsWorldMap()
        {
            return IsWorldMap;
        }
        public void SetName(string newName)
        {
            mCourseName = newName;
        }


        public void renameAreaFromMenu()
        {
            foreach (CourseArea mArea in mAreas)
            {
                string areaName = mArea.mAreaName;
                mArea.mAreaName = mCourseName.Split("_")[0] + "_" + areaName.Split("_")[1];
            }
        }


        public void LoadFromRomFS()
        {
            var courseFilePath = FileUtil.FindContentPath(Path.Combine("BancMapUnit", $"{mCourseName}.bcett.byml.zs"));
            var stageParamFilePath = FileUtil.FindContentPath(Path.Combine("Stage", "StageParam", $"{mCourseName}.game__stage__StageParam.bgyml"));

            /* grab our course information file */
            Byml.Byml courseInfo = new Byml.Byml(new MemoryStream(FileUtil.DecompressFile(courseFilePath)));
            Byml.Byml stageParam = new Byml.Byml(new MemoryStream(File.ReadAllBytes(stageParamFilePath)));

            var stageParamRoot = (BymlHashTable)stageParam.Root;
            var root = (BymlHashTable)courseInfo.Root;


            IsOneAreaCourse = ((BymlNode<string>)stageParamRoot["Category"]).Data == "Course1Area";
            if (IsOneAreaCourse == true)
            {
                overWriteCourseParam = true;
            }

            try
            {
                mStageReferences = (BymlArrayNode)root["RefStages"];

                for (int i = 0; i < mStageReferences.Length; i++)
                {
                    string stageParamPath = ((BymlNode<string>)mStageReferences[i]).Data.Replace("Work/", "").Replace(".gyml", ".bgyml");
                    string stageName = Path.GetFileName(stageParamPath).Split(".game")[0];
                    mAreas.Add(new CourseArea(stageName, false));

                }
            }
            catch
            {
                mAreas.Add(new CourseArea(mCourseName, false));

            }

            mAreas = mAreas
                .OrderBy(area =>
                {
                    string name = area.GetName();

                    if (name.EndsWith("_Main"))
                        return 0;

                    if (name.Contains("_Sub"))
                    {
                        string numStr = name.Substring(name.LastIndexOf("_Sub") + 4);
                        if (int.TryParse(numStr, out int num))
                            return num;
                    }
                    return int.MaxValue;
                })
                .ToList();

            if (root.ContainsKey("Links") && !IsOneAreaCourse)
            {
                if (root["Links"] is BymlArrayNode linksArr)
                {
                    mGlobalLinks = new(linksArr);
                    return;
                }
            }

            mGlobalLinks = new(new BymlArrayNode());

        }
        public void AddArea()
        {
            int areaCount = mAreas.Count;
            var usedNumbers = new HashSet<int>();
            string NewAreaName = "";
            string areaName = "";
            NewAreaName = mCourseName.Replace("_Course", "");
            if (areaCount != 1)
            {
                foreach (var area in mAreas)
                {
                    string name = area.GetName();

                    if (name.StartsWith(NewAreaName + "_Sub"))
                    {
                        string numStr = name.Substring((NewAreaName + "_Sub").Length);
                        if (int.TryParse(numStr, out int num))
                            usedNumbers.Add(num);
                    }
                }


                int nextNumber = 1;
                while (usedNumbers.Contains(nextNumber))
                    nextNumber++;

                areaName = $"{NewAreaName}_Sub{nextNumber}";

            }
            else if (areaCount == 1)
            {
                areaName = $"{NewAreaName}_Sub1";
            }

            string testPath = FileUtil.FindContentPath(Path.Combine("BancMapUnit", $"{areaName}.bcett.byml.zs"));
            bool overrideVanilla = File.Exists(testPath);
            mAreas.Add(new CourseArea(areaName, overrideVanilla));
            renameArea();

        }

        public void renameArea()
        {
            string oldAreaName = mAreas[0].mAreaName;
            if (GetAreaCount() > 1 && IsOneAreaCourse)
            {
                IsOneAreaCourse = false;
                Catergory = "Course";
                if (oldAreaName.EndsWith("_Course"))
                {
                    oldAreaName = mCourseName.Replace("_Course", "");
                    mAreas[0].mAreaName = oldAreaName + "_Main";
                }

            }
            else if (GetAreaCount() == 1 && !IsOneAreaCourse)
            {
                IsOneAreaCourse = true;
                Catergory = "Course1Area";

                if (oldAreaName.EndsWith("Main"))
                {
                    mAreas[0].mAreaName = oldAreaName.Replace("Main", "Course");
                }
                else
                {
                    oldAreaName = Regex.Replace(oldAreaName, "_Sub[0-9]+$", "");
                    oldAreaName = oldAreaName + "_Course";
                    mAreas[0].mAreaName = oldAreaName;
                }
            }
        }
        public List<CourseArea> GetAreas() => mAreas;

        public CourseArea GetArea(int idx) => mAreas.ElementAt(idx);

        public CourseArea? GetArea(string name)
        {
            foreach (CourseArea area in mAreas)
                if (area.GetName() == name)
                    return area;

            return null;
        }

        public int GetAreaCount()
        {
            return mAreas.Count;
        }

        public void AddGlobalLink()
        {
            if (mGlobalLinks == null)
            {
                Logger.Logger.LogWarning("Course", "mGlobalLinks == null! (AddGlobalLink)");
                return;
            }

            CourseLink link = new("Reference");
            mGlobalLinks.mLinks.Add(link);
        }

        public void RemoveGlobalLink(CourseLink link)
        {
            if (mGlobalLinks == null)
            {
                Logger.Logger.LogWarning("Course", "mGlobalLinks == null! (RemoveGlobalLink)");
                return;
            }

            mGlobalLinks.mLinks.Remove(link);
        }

        public CourseLinkHolder? GetGlobalLinks()
        {
            return mGlobalLinks;
        }

       
        public void UpdateStageParam(string dir)
        {
            if (!overWriteCourseParam)
                return;

            var stageParamFilePath = FileUtil.FindContentPath(Path.Combine("Stage", "StageParam", $"{mCourseName}.game__stage__StageParam.bgyml"));
            bool noFileFound = !File.Exists(stageParamFilePath);

            Byml.Byml stageParam = new Byml.Byml(new MemoryStream(File.ReadAllBytes(stageParamFilePath)));

            var oldRoot = (BymlHashTable)stageParam.Root;
            var newRoot = new BymlHashTable();


            foreach (var pair in oldRoot.Pairs)
            {
                if (pair.Name == "Category")
                    continue;

                newRoot.AddNode(pair.Id, pair.Value, pair.Name);
            }

            newRoot.AddNode(BymlNodeId.String, BymlUtil.CreateNode(Catergory), "Category");
            stageParam.Root = newRoot;
            string outPath = Path.Combine(dir, "Stage/StageParam", $"{mCourseName}.game__stage__StageParam.bgyml");

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            using (var ms = new MemoryStream())
            {
                stageParam.Save(ms);
                File.WriteAllBytes(outPath, ms.ToArray());
            }
        }

        public CourseActor? ResolveActorByHash(ulong hash)
        {
            foreach (var area in mAreas)
            {
                var actor = area.GetActorByHash(hash);
                if (actor != null)
                    return actor;
            }
            return null;
        }

        public void SaveCourse(RSTB resource_table, string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            BymlHashTable root = new();

            root.AddNode(BymlNodeId.Array, mGlobalLinks.SerializeToArray(), "Links");

            if (mStageReferences != null)
            {
                BymlArrayNode refArr = new();
                foreach (CourseArea area in mAreas)
                {
                    string refPath = $"Work/Stage/StageParam/{area.GetName()}.game__stage__StageParam.gyml";
                    refArr.AddNodeToArray(BymlUtil.CreateNode(refPath));
                }

                root.AddNode(BymlNodeId.Array, refArr, "RefStages");
            }

            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);

            var decomp_size = (uint)mem.Length;


            //Compress and save the course          
            string levelPath = Path.Combine(folder, $"{mCourseName}.bcett.byml.zs");
            File.WriteAllBytes(levelPath, FileUtil.CompressData(mem.ToArray()));

            //Update resource table
            // filePath is a key not an actual path so we cannot use Path.Combine
            resource_table.SetResource($"BancMapUnit/{mCourseName}.bcett.byml", decomp_size);
        }


        string mCourseName;
        List<CourseArea> mAreas;
        BymlArrayNode mStageReferences;
        CourseLinkHolder? mGlobalLinks;
        public CourseInfo mCourseInfo;
        public WorldMapInfo mWorldInfo;
        public MapAnalysisInfo mMapAnalysisInfo;
        public StageLoadInfo mStageLoadInfo;
        public static bool IsOneAreaCourse;
        public static string Catergory;
        public static bool updateStageParam;
        private bool overWriteCourseParam;
        public static bool IsWorldMap;
    }
}



