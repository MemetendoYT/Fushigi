using Fushigi.param;
using Fushigi.ui;
using Fushigi.ui.undo;
using Fushigi.util;
using System.Numerics;


namespace Fushigi.course.distance_view
{
    public class DistantViewManager
    {
        private Dictionary<string, Matrix4x4> LayerMatrices = new Dictionary<string, Matrix4x4>();
        public DVLayerParamTable ParamTable = new DVLayerParamTable();

        public CourseActor DVLocator;

        public float ScrollSpeedX = -0.025f;
        public float ScrollSpeedY = 0f;

        public DistantViewManager(CourseArea area)
        {
            PrepareDVLocator(area);
        }

        public void PrepareDVLocator(CourseArea area)
        {
            ParamTable.LoadDefault();

            CheckForDVLocator(area, false);

            LayerMatrices.Clear();
            foreach (var layer in this.ParamTable.Layers)
                LayerMatrices.Add(layer.Key, Matrix4x4.Identity);
        }

        internal void CheckForDVLocator(CourseArea area, bool commit, CourseAreaEditContext mEditContext = null)
        {
            foreach (var actor in area.GetActors())
            {
                if (actor.mPackName == "DVBasePosLocator")
                {
                    Reload(actor, commit, mEditContext);
                }
            }
        }

        internal void Reload(CourseActor actor, bool commit, CourseAreaEditContext mEditContext = null)
        {
            float oldScrollSpeedX = ScrollSpeedX;
            float oldScrollSpeedY = ScrollSpeedY;
            DVLayerParamTable oldParamTable = ParamTable;

            DVLocator = actor;
            Console.WriteLine("updating");
            
            if (DVLocator.mActorParameters.ContainsKey("TimeScrollRateX"))
                ScrollSpeedX = (float)DVLocator.mActorParameters["TimeScrollRateX"];

            if (DVLocator.mActorParameters.ContainsKey("TimeScrollRateY"))
                ScrollSpeedY = (float)DVLocator.mActorParameters["TimeScrollRateY"];

            if (DVLocator.mActorParameters.ContainsKey("DVLayerParamName"))
            {
                string layer_param = (string)DVLocator.mActorParameters["DVLayerParamName"];
                if (!string.IsNullOrEmpty(layer_param))
                {
                    var newTable = new DVLayerParamTable();
                    newTable.Load(layer_param);
                    ParamTable = newTable;
                }
            }

            if (commit)
            {
                mEditContext.CommitAction(new PropertyFieldsSetUndo(
                this,
                [
                    ("ScrollSpeedX", oldScrollSpeedX),
                    ("ScrollSpeedY", oldScrollSpeedY),
                    ("ParamTable", oldParamTable)
                ],
                    $"{IconUtil.ICON_RUNNING} Updated DV Scroll Rate"
                ));
            }
        }

        public void UpdateMatrix(string layer, ref Matrix4x4 matrix)
        {
            if (LayerMatrices.ContainsKey(layer))
                matrix *= LayerMatrices[layer];
        }

        public void Calc(Vector3 camera_pos)
        {
            foreach (var layer in ParamTable.Layers.Keys)
            {
                var scroll_config = ParamTable.Layers[layer];

                var locator_pos = DVLocator != null ? DVLocator.mTranslation : Vector3.Zero;
                Console.WriteLine(locator_pos + " " + DVLocator.mName);
                //Place via base locator pos + camera
                
                //Distance between dv locator and camera
                if (UserSettings.GetUseNewCamera())
                {
                    Vector2 distance = new(camera_pos.X - locator_pos.X, camera_pos.Y - locator_pos.Y);
                    Vector2 movement_ratio = new Vector2(1f, 1f) - scroll_config;
                    Vector2 scroll_time_rate = new(0.960f - ScrollSpeedX, 1.9f - ScrollSpeedY);

                    float posX = 0;//, posY = 0;

                    if (scroll_config.X != 1 && scroll_time_rate.X != 0)
                        posX = distance.X * movement_ratio.X * scroll_time_rate.X;
                    //if (scroll_config.X != 1 && scroll_time_rate.Y != 0)
                    //    posY = distance.Y * movement_ratio.Y * scroll_time_rate.Y;

                    LayerMatrices[layer] = Matrix4x4.CreateTranslation(posX + movement_ratio.X * scroll_time_rate.X * 2, movement_ratio.Y * scroll_time_rate.Y, 0);
                } else
                {
                    Vector2 distance = new Vector2(camera_pos.X - locator_pos.X, camera_pos.Y - locator_pos.Y);
                    Vector2 movement_ratio = new Vector2(1.0f) - scroll_config;
                    Vector2 scroll_time_rate = new Vector2(1.0f - ScrollSpeedX, 1.0f - ScrollSpeedY);

                    float posX = 0, posY = 0;

                    if (scroll_config.X != 1 && scroll_time_rate.X != 0)
                        posX = distance.X * movement_ratio.X * scroll_time_rate.X;
                    if (scroll_config.X != 1 && scroll_time_rate.Y != 0)
                        posY = distance.Y * movement_ratio.Y * scroll_time_rate.Y;

                    LayerMatrices[layer] = Matrix4x4.CreateTranslation(posX, posY, 0);
                }
            }
        }
    }
}
