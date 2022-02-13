using Microsoft.Xna.Framework;

namespace MoreCommands.Extensions;

public static class Vector2Extensions
{
    public static (float x, float y) ToTuple(this Vector2 self) => (self.X, self.Y);
    
    public static (int x, int y) ToIntTuple(this Vector2 self) => ((int) self.X, (int) self.Y);
}