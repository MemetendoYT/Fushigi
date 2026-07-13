using Fushigi.Bfres.Common;
using System.Reflection.PortableExecutable;

namespace Fushigi.Bfres
{
    [Serializable]
    public class MaterialAnimation
    {
        public string Name { get; set; }
        public int FrameCount { get; set; }
        public List<MaterialAnimConfigs> MaterialConfigs { get; set; }
        public void Read(BinaryReader reader)
        {
            //var resFile = new BfresFile(bfresFilePath);
            //MaterialAnim matAnim = resFile.MaterialAnims["PatternColor"];

        }
    }
    [Serializable]
    public class MaterialAnimConfigs
    {
        public string Name { get; set; }
        public int FrameCount { get; set; }
    }
}

