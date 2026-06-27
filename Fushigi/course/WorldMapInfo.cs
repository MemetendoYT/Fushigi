using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.rstb;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    [Serializable]
    public class WorldMapInfo : BymlObject
    {
        //public string CourseDifficulty { get; set; } //
        //public string CourseKind { get; set; } //
        //public string CoursePlayerMorphType { get; set; } //
        //public string CourseNameLabel { get; set; } //
        //public string CourseScreenCaptureMainActor { get; set; } //
        //public string CourseStartXLinkKey { get; set; } //
        //public string CourseThumbnailPath { get; set; } //
        //public int CourseTimer { get; set; } //
        //public string CourseTimerType { get; set; } //
        //public string DemoCourseKind { get; set; } //
        //public string GiveBadgeIdOnCourseClear { get; set; } //
        //public int GlobalCourseId { get; set; } //
        //public bool IsCourseTimerAutoStart { get; set; } //
        //public bool IsDashMiniCourse { get; set; } //
        //public bool IsExistWonderQuiz { get; set; } //
        //public bool IsInvisibleBadgeSetShadow { get; set; } //
        //public bool IsUseTheEndUI { get; set; } //
        //public string NeedBadgeIdEnterCourse { get; set; } //
        //public bool NoNeedRetrySuggestBadge { get; set; } //
        //public string RaceCourseType { get; set; } //
        //public List<string> SuggestBadgeList { get; set; } //
        //public string SuggestBadgeReplaceLabel { get; set; } //
        //public List<string> TipsTags { get; set; } //
        //public List<TipInfo> TipsInfo { get; set; } //
        [BymlProperty("CourseTable")]
        public List<CourseTable> Courses { get; set; }

        [BymlProperty("GateTable")]
        public List<GateTable> Gates { get; set; }


        public WorldMapInfo(string name)
        {

            if (!Course.IsWorldMap)
                return; 

            var courseFilePath = FileUtil.FindContentPath(Path.Combine("Stage", "WorldMapInfo", $"{name}.game__stage__WorldMapInfo.bgyml"));
            var byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(courseFilePath)));

            this.Load((BymlHashTable)byml.Root);
        }

        public void Save(RSTB resource_table, string folder, string courseName)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var root = this.Serialize();

            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);

            var decomp_size = (uint)mem.Length;

            //Compress and save the course area           
            string levelPath = Path.Combine(folder, $"{courseName}.game__stage__WorldMapInfo.bgyml");
            File.WriteAllBytes(levelPath, mem.ToArray());

            //Update resource table
            // filePath is a key not an actual path so we cannot use Path.Combine
            resource_table.SetResource($"Stage/WorldMapInfo/{courseName}.game__stage__WorldMapInfo.bgyml", decomp_size);
        }

        [Serializable]
        public class CourseTable
        {
            public string Key { get; set; }
        }

        public class GateTable
        {
            public int GateNo { get; set; }
            public int Price { get; set; }
            public string BalloonMsgLabel { get; set; }
        }
    }
}
