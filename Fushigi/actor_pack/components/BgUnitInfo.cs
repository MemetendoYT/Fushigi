using Fushigi.Byml.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class BgUnitInfo
    {
        public (string bfresName, string modelName) GetModelInfo()
        {
            var m = Regex.Match(Fmdb, "Work/Model/Unit/(.*)/output/(.*).fmdb");

            return (m.Groups[1].Value, m.Groups[2].Value);
        }
        public string Fmdb { get; set; }

        public string DecoInfo { get; set; }

        public List<AttrListEntry> AttrList { get; set; }

        public Dictionary<string, string> GetMaterialNames() => AttrList.ToDictionary(x => x.AttributeConvert(), x => x.MaterialName);

    
        [Serializable]
        public class AttrListEntry
        {
            public object DrawAttr { get; set; }
            public string MaterialName { get; set; }



            private static readonly Dictionary<uint, string> DrawAttrNames = new()
            {
                { 741904279, "Wall" },
                { 470423374, "Edge" },
                { 2155127678, "Ground" }
            };

            public string AttributeConvert()
            {
                string attribute = "";
                if(DrawAttr is uint)
                    attribute = DrawAttrNames[(uint)DrawAttr];
                else 
                    attribute = DrawAttr.ToString();

                return attribute;
            }

        }

        public List<string> MaterialPresetList { get; set; }

        public bool UseLongParts { get; set; }
    }
}
