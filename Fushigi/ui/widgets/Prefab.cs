using Fushigi.course;
using Fushigi.ui;
using Fushigi.ui.widgets;

public class Prefab
{
    internal static void SavePrefab(CourseAreaEditContext mEditContext, string prefabName, CourseArea mArea)
    {
        var median = System.Numerics.Vector3.Zero;

        if (mEditContext.GetSelectedObjects<CourseActor>().Count() > 0 || mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().Count() > 0)
        {
            List<CourseActor> actors = mEditContext.GetSelectedObjects<CourseActor>().ToList();
            List<CourseActor> copiedActors = new List<CourseActor>();

            List<CourseRail.CourseRailPoint> courseRailPoints = mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().ToList();
            List<CourseRail> courseRails = new List<CourseRail>();
            List<CourseRail> courseRailsClone = new List<CourseRail>();
            foreach (var point in courseRailPoints)
            {
                if (!courseRails.Contains(point.mParent))
                    courseRails.Add(point.mParent);
            }

            foreach (var actor in actors)
                copiedActors.Add(actor.ClonePrefab(mArea));

            foreach (var rail in courseRails)
            {
                courseRailsClone.Add(rail.CloneRail(mArea));
            }

            foreach (CourseActor actor in copiedActors)
                median += actor.mTranslation;

            median /= actors.Count;

            foreach (var actor in copiedActors)
            {
                actor.mTranslation.X -= median.X;
                actor.mTranslation.Y -= median.Y;
            }

            foreach (var rail in courseRailsClone)
            {
                foreach (var point in rail.mPoints)
                {
                    point.mTranslation.X -= median.X;
                    point.mTranslation.Y -= median.Y;

                    if (point.mIsCurve)
                    {
                        point.mControl.mTranslation.X -= median.X;
                        point.mControl.mTranslation.Y -= median.Y;
                    }
                }
            }

            mArea.SaveActorsToPrefab(copiedActors, actors, prefabName, courseRailsClone, courseRails);
        }
    }

    internal static async Task PrefabPopup(CourseAreaEditContext mEditContext, CourseArea area)
    {
        var result = await SavePrefabDialog.ShowDialog(MainWindow.mModalHost, "Save Prefab", "Enter name for this prefab");

        if (result.Result == SavePrefabDialog.DialogResult.Yes)
            SavePrefab(mEditContext, result.PrefabName, area);

    }
}
