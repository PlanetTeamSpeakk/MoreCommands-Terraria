using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using MoreCommands.Extensions;
using MoreCommands.Utils;
using Terraria;

namespace MoreCommands.IL.Manipulations;

// Adds the ability to click anywhere on the map while holding ctrl to teleport there.
public class MapTeleportManipulation : ILManipulation
{
    public override MethodBase Target => Util.GetMethod(typeof(Main), "DrawMap", false, false);
    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(inst => inst.MatchStloc(134) && inst.Previous.MatchConvI4() && inst.Previous.Previous.MatchAdd() && inst.Previous.Previous.Previous.MatchLdloc(9) &&
                            inst.Previous.Previous.Previous.Previous.MatchDiv() && inst.Previous.Previous.Previous.Previous.Previous.MatchLdloc(5))
    };
    
    public override void Inject(ILCursor c)
    {
        c.EmitDelegate(delegate(int x, int y)
        {
            if (!Main.mouseLeft || !Main.mouseLeftRelease || !Main.keyState.IsKeyDown(Keys.LeftControl) && !Main.keyState.IsKeyDown(Keys.RightControl)) return;
            
            Util.SendCommand($"/teleport {x} {y}");
            Main.mapFullscreen = false;
        }, ILParameter.LoadLoc(133), ILParameter.LoadLoc(134)); // Load the X and Y values.
    }
}