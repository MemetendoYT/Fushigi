using System.Diagnostics.Contracts;

namespace OpenAbility.ImGui.Nodes;


/*
#define IM_COL32_R_SHIFT    0
   #define IM_COL32_G_SHIFT    8
   #define IM_COL32_B_SHIFT    16
   #define IM_COL32_A_SHIFT    24
   #define IM_COL32_A_MASK     0xFF000000
   #endif
   #endif
   #define IM_COL32(R,G,B,A)    (((ImU32)(A)<<IM_COL32_A_SHIFT) | ((ImU32)(B)<<IM_COL32_B_SHIFT) | ((ImU32)(G)<<IM_COL32_G_SHIFT) | ((ImU32)(R)<<IM_COL32_R_SHIFT))
 */

public static class ImUtil
{
	public const int ImCol32RShift = 0;
	public const int ImCol32GShift = 8;
	public const int ImCol32BShift = 16;
	public const int ImCol32AShift = 24;
	
	[Pure]
	public static uint ImCol32(byte r, byte g, byte b, byte a)
	{
		return unchecked((uint)((a << ImCol32AShift) | (b << ImCol32BShift) | (g << ImCol32GShift) | (r << ImCol32RShift)));
	}
}
