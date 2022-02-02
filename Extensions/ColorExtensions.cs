using Microsoft.Xna.Framework;

namespace MoreCommands.Extensions;

public static class ColorExtensions
{
    public static Color WithAlpha(this Color self, float alpha) => new(self.R, self.G, self.B, (int) alpha * byte.MaxValue);
}