using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MoreCommands.Extensions;
using MoreCommands.Misc.Styling;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace MoreCommands.IL.Manipulations;

public class TextSnippetStylingManipulation : ILManipulation
{
    public override MethodBase Target => typeof(ChatManager).GetMethods().First(m => m.IsStatic && m.IsPublic && m.Name == "DrawColorCodedString" && m.GetParameters()[2].ParameterType == typeof(TextSnippet[]));

    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(inst => inst.MatchLdarg(1) && inst.Next.MatchLdloc(15) && inst.Next.Next.MatchLdloc(16)),
        Move(inst => inst.MatchStloc(17))
    };
    
    public override void Inject(ILCursor c)
    {
        c.EmitDelegate((SpriteBatch spriteBatch, TextSnippet snippet, Vector2 start, Vector2 dims, Color baseColor, bool ignoreColors) =>
        {
            if (snippet is not StyledTextSnippet { IsUnderline: true }) return;

            int x = (int) start.X;
            int y = (int) (start.Y + dims.Y - 8);
            Color colour = ignoreColors ? baseColor : snippet.GetVisibleColor();
            if (ignoreColors) colour.A = 196;
            
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, (int) dims.X, 2), new Rectangle(0, 0, 1, 1), colour);
        }, ILParameter.LoadArg(0), ILParameter.LoadLoc(9), ILParameter.LoadLoc(2), ILParameter.LoadLoc(17), ILParameter.LoadArg(4), ILParameter.LoadArg(10));
    }
}