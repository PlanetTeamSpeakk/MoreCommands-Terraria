using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace MoreCommands.UI;

public class UIItemSlotWrapper : UIElement
{
    public bool IsEmpty => _item is null || _item.type == ItemID.None;
    private Item _item;
    private Item[] _inv;
    private readonly int _context;
    private readonly float _scale;

    public UIItemSlotWrapper(int context = ItemSlot.Context.BankItem, float scale = 1f)
    {
        _context = context;
        _scale = scale;

        Asset<Texture2D> inventoryBack9 = TextureAssets.InventoryBack9;
        Width.Set(inventoryBack9.Width() * scale, 0f);
        Height.Set(inventoryBack9.Height() * scale, 0f);
        
        Reset();
    }

    public void SetItem(Item item)
    {
        _item = item;
        _inv = new[] {_item};
    }

    public bool CheckInv(Item[] inv) => inv == _inv;
    
    public void Reset()
    {
        _item = new Item();
        _item.SetDefaults();

        _inv = new[] {_item};
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        float oldScale = Main.inventoryScale;
        Main.inventoryScale = _scale;
        Rectangle rectangle = GetDimensions().ToRectangle();

        if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
        {
            Main.LocalPlayer.mouseInterface = true;
            ItemSlot.Handle(_inv, _context);
            _item = _inv[0];
        }
        ItemSlot.Draw(spriteBatch, ref _item, _context, rectangle.TopLeft());

        Main.inventoryScale = oldScale;
    }
}