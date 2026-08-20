using Fushigi.course;
using Fushigi.ui.SceneObjects.bgunit;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui
{

    public class TileRebuildRevertable : IRevertable
    {
        public string Name { get; }

        private readonly CourseUnit unit;

        public TileRebuildRevertable(CourseUnit unit, string name = "Tile Rebuild")
        {
            this.unit = unit;
            Name = name;
        }

        public IRevertable Revert()
        {
            Fushigi.ui.SceneObjects.bgunit.BGUnitRailSceneObj.rebuildUnit(unit);
            return this;
        }
    }



}
