using Fushigi.gl;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Xml.Linq;

public class Sprites
{
    public static Dictionary<string, GLTexture> ActorSprites = new();

    public static Dictionary<string, string> SpriteAliases = new()
    {
        { "BlockRengaItem", "BlockRenga" },
        { "BlockRengaItemBindable", "BlockRenga" },
        { "BlockRengaLight", "BlockRenga" },
        { "ObjectTalkingFlowerAfar", "ObjectTalkingFlower" },
        { "ObjectTalkingFlowerS", "ObjectTalkingFlower"},
        { "NoteObjectCoinYellow", "ObjectCoinYellow" },
        { "ObjectMiniFlowerWater", "ObjectMiniFlower" },
        { "ObjectMiniFlowerInAir", "ObjectMiniFlower" },
        { "ObjectMiniFlowerApproach", "ObjectMiniFlower" },
        { "ObjectMiniFlowerInBlock", "ObjectMiniFlower" },
        { "ObjectPropellerFlowerForCourse", "ObjectPropellerFlower" }
    };

    public static Dictionary<string, Vector2> OverrideSize = new()
    {
        { "ObjectMiniFlowerWater", new Vector2(4, 4) },
        { "ObjectMiniFlowerApproach", new Vector2(4, 4) },
        { "EnemyKuribo", new Vector2(2, 2) },
        { "ObjectPropellerFlowerForCourse", new Vector2(3, 3) },
        { "ObjectTalkingFlower", new Vector2(4, 4)},
    };


    public Sprites(GL gl)
    {

    string path = "res/ActorIcons/";
        string[] files = Directory.GetFiles(path, "*.png*");

        foreach (string file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file).Split(".bfres")[0];
            ActorSprites[name] = GLTexture2D.Load(gl, file);
        }
    }
}
