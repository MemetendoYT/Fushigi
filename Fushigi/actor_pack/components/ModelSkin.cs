using Fushigi.Byml.Serializer;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class ModelSkin
    {
        public Dictionary<string, Entry> ModelInfoList { get; set; }

        [BymlIgnore]
        public Dictionary<string, string> Anims { get; set; } = new();

        [BymlIgnore]
        public Dictionary<string, ModelInfo> Models { get; set; } = new();

        [Serializable]
        public class Entry
        {
            public string ModelInfoPath { get; set; }
        }
    }
}