using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MoreCommands.UI;

public class DisposalUI : UIState
{
    private readonly UIItemSlotWrapper[] _slots = new UIItemSlotWrapper[40];
    private bool _wasInvOpen = false;

    public override void OnInitialize()
    {
        UIPanel panel = new()
        {
            HAlign = .5f,
            VAlign = .5f,
            Width = new StyleDimension(600, 0),
            Height = new StyleDimension(310, 0),
            BackgroundColor = Color.Firebrick * .7f
        };
        
        panel.Append(new UIText("Disposal", 0.7f, true)
        {
            Left = new StyleDimension(15, 0),
            Top = new StyleDimension(6, 0)
        });
        
        panel.Append(new UIImage(MoreCommands.Instance.Assets.Request<Texture2D>("Assets/TrashCan", AssetRequestMode.ImmediateLoad))
        {
            HAlign = .5f,
            VAlign = .5f,
            ImageScale = 3,
            Color = new Color(255, 255, 255, 127)
        });

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = new UIItemSlotWrapper
            {
                Left = new StyleDimension(i % 10 * 55 + 15, 0),
                Top = new StyleDimension(i / 10 * 55 + 40, 0)
            };
            panel.Append(_slots[i]);
        }
        
        panel.Append(new UIButton("Close", (_, _) => Close())
        {
            HAlign = .5f,
            BackgroundColor = panel.BackgroundColor,
            Top = new StyleDimension(300 - 40, 0)
        });
        Append(panel);

        OnUpdate += _ =>
        {
            if (ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;
            
            if (!Main.playerInventory) Close();
        };
    }

    public bool OnShiftClick(Item[] inventory, int slot)
    {
        if (_slots.Any(itemSlot => itemSlot.CheckInv(inventory))) // Shift-clicking on slot in the disposal, attempt to move back to inv.
        {
            if (!Main.LocalPlayer.inventory.Any(item => item is null || item.type == ItemID.None))
                return false; // No empty slot, just set the item to the cursor item.

            for (int i = 0; i < Main.LocalPlayer.inventory.Length; i++)
            {
                Item item = Main.LocalPlayer.inventory[i];
                if (item is not null && item.type != ItemID.None) continue;
                
                Main.LocalPlayer.inventory[i] = inventory[slot];
                inventory[slot] = new Item();

                SoundEngine.PlaySound(SoundID.Grab);
                return true;
            }

            return false; // Should not be able to be reached.
        }
        
        UIItemSlotWrapper itemSlot = _slots.FirstOrDefault(itemSlot => itemSlot.IsEmpty);
        if (itemSlot is null) return false;

        itemSlot.SetItem(inventory[slot]);
        inventory[slot] = new Item();
        
        SoundEngine.PlaySound(SoundID.Grab);
        return true;
    }

    public override void OnActivate()
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        _wasInvOpen = Main.playerInventory;
        Main.playerInventory = true;
        Empty();
    }

    public override void OnDeactivate() => Empty();

    private void Empty()
    {
        foreach (UIItemSlotWrapper slot in _slots)
            slot.Reset();
    }

    public void Close()
    {
        MoreCommands.DisposalInterface.SetState(null);
        Deactivate();
        
        if (!_wasInvOpen) Main.playerInventory = false;
        
        SoundEngine.PlaySound(SoundID.MenuClose);
    }
}