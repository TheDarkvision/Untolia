using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Untolia.Core.Inventory;

namespace Untolia.Core.UI;

public sealed class InventoryMenu : Menu
{
    private const float NavCd = 0.12f;
    private const float TabCd = 0.16f;
    private const float ActCd = 0.18f;

    // Viewport metrics for keeping selection visible
    private const float RowHeight = 28f;

    // Selection per tab (index into the live stacks list for that tab)
    private readonly Dictionary<InvTab, int> _sel = new()
    {
        { InvTab.Items, 0 },
        { InvTab.Equipment, 0 },
        { InvTab.KeyItems, 0 }
    };

    private float _actCooldown;
    private int _listContentTop; // pixel Y where first row starts (row 0 top)

    private float _listNavCooldown;

    // Scrolling and input debounce (separate from Menu base to avoid conflicts)
    private float _listScroll;
    private int _listViewBottom; // pixel Y of visible list area bottom
    private int _listViewTop; // pixel Y of visible list area top
    private int _prevWheel;

    private InvTab _tab = InvTab.Items;
    private float _tabCooldown;

    public InventoryMenu()
    {
        SetupLeftMenu();
        PositionMenu();
    }

    private void SetupLeftMenu()
    {
        _items.Clear();
        AddMenuItem(new MenuItem("Use / Equip", UseSelected));
        AddMenuItem(new MenuItem("Sort", SortCurrent));
        AddMenuItem(new MenuItem("Back", BackToGameMenu));
    }

    private void PositionMenu()
    {
        // Left-side, full-height panel with a nice margin
        var margin = 24f;
        var panelWidth = (int)Math.Clamp(Globals.ScreenSize.X * 0.28f, 280f, 420f);
        var panelHeight = Globals.ScreenSize.Y - (int)(margin * 2);
        Size = new Vector2(panelWidth, panelHeight);
        Position = new Vector2(margin, margin);
    }

    protected override void CloseMenu()
    {
        BackToGameMenu();
    }

    public override void OnAdded()
    {
        base.OnAdded();
        _prevWheel = Mouse.GetState().ScrollWheelValue;
        _listScroll = 0f;
        _listNavCooldown = _tabCooldown = _actCooldown = 0.12f;
        // Make sure selection is valid with current inventory
        EnsureSelectionVisible();
    }

    public override void Update(float deltaTime)
    {
        _listNavCooldown = Math.Max(0, _listNavCooldown - deltaTime);
        _tabCooldown = Math.Max(0, _tabCooldown - deltaTime);
        _actCooldown = Math.Max(0, _actCooldown - deltaTime);

        var kb = Keyboard.GetState();
        var ms = Mouse.GetState();

        // Switch tabs: Left/Right or Q/E
        if (_tabCooldown <= 0f && (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.Q)))
        {
            _tab = (InvTab)(((int)_tab + 3 - 1) % 3);
            _tabCooldown = TabCd;
            EnsureSelectionVisible();
        }

        if (_tabCooldown <= 0f && (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.E)))
        {
            _tab = (InvTab)(((int)_tab + 1) % 3);
            _tabCooldown = TabCd;
            EnsureSelectionVisible();
        }

        // Navigate list: Up/Down or W/S
        if (_listNavCooldown <= 0f && (kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W)))
        {
            var stacks = GetCurrentStacks();
            if (stacks.Count > 0)
            {
                _sel[_tab] = (_sel[_tab] - 1 + stacks.Count) % stacks.Count;
                _listNavCooldown = NavCd;
                EnsureSelectionVisible();
            }
        }

        if (_listNavCooldown <= 0f && (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S)))
        {
            var stacks = GetCurrentStacks();
            if (stacks.Count > 0)
            {
                _sel[_tab] = (_sel[_tab] + 1) % stacks.Count;
                _listNavCooldown = NavCd;
                EnsureSelectionVisible();
            }
        }

        // Mouse wheel scroll
        var wheel = ms.ScrollWheelValue;
        var delta = wheel - _prevWheel;
        _prevWheel = wheel;
        if (delta != 0)
        {
            _listScroll -= delta / 120f * 40f; // pixels per notch
            if (_listScroll < 0f) _listScroll = 0f;
        }

        // Enter uses/equips, Escape backs out
        if (_actCooldown <= 0f && kb.IsKeyDown(Keys.Enter))
        {
            UseSelected();
            _actCooldown = ActCd;
        }

        if (_actCooldown <= 0f && kb.IsKeyDown(Keys.Escape))
        {
            BackToGameMenu();
            _actCooldown = ActCd;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Left panel (actions)
        var leftRect = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
        DrawLeftPanel(spriteBatch, leftRect);

        // Right pane fills remaining screen area with same vertical margins
        var margin = 24;
        var rightRect = new Rectangle(leftRect.Right + margin, margin,
            Globals.ScreenSize.X - leftRect.Right - margin * 2, Globals.ScreenSize.Y - margin * 2);
        DrawRightPane(spriteBatch, rightRect);
    }

    private void DrawLeftPanel(SpriteBatch sb, Rectangle panelRect)
    {
        // Background
        sb.Draw(UIAssets.PixelTexture, panelRect, Color.Black * 0.80f);
        DrawBorder(sb, panelRect, 3, new Color(255, 255, 255, 30));

        // Header
        var headerRect = new Rectangle(panelRect.X + 6, panelRect.Y + 6, panelRect.Width - 12, 48);
        sb.Draw(UIAssets.PixelTexture, headerRect, new Color(60, 60, 90, 160));
        var t = "INVENTORY";
        var ts = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, t);
        sb.DrawStringSafe(UIAssets.DefaultFont, t,
            new Vector2(headerRect.X + 12, headerRect.Y + (headerRect.Height - ts.Y) / 2f), Color.White);

        // Actions list (from base _items, but drawn in our style)
        float y = headerRect.Bottom + 16;
        var lh = 36f;
        float leftPad = panelRect.X + 24;
        float rightPad = panelRect.Right - 24;

        for (var i = 0; i < _items.Count; i++)
        {
            var isSelected = i == _selectedIndex;
            var label = _items[i].Text;
            var sz = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, label);

            if (i == _items.Count - 1) DrawSeparator(sb, leftPad, rightPad, y - 8);

            if (isSelected)
            {
                var hl = new Rectangle(panelRect.X + 6, (int)(y - 6), panelRect.Width - 12, (int)lh);
                sb.Draw(UIAssets.PixelTexture, hl, new Color(90, 90, 120, 160));
                var accent = new Rectangle(panelRect.X + 6, (int)(y - 6), 4, (int)lh);
                sb.Draw(UIAssets.PixelTexture, accent, Color.CornflowerBlue);
            }

            var c = isSelected ? Color.White : new Color(220, 220, 220);
            var pos = new Vector2(leftPad, y + (lh - sz.Y) / 2f - 4f);
            sb.DrawStringSafe(UIAssets.DefaultFont, label, pos, c);

            y += lh + 4f;
        }

        // Bottom hint
        var hint = "←/→: Tabs    ↑/↓: Select    Enter: Action    Esc: Back";
        var hsz = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, hint);
        var hintRect = new Rectangle(panelRect.X + 6, panelRect.Bottom - 40, panelRect.Width - 12, 34);
        sb.Draw(UIAssets.PixelTexture, hintRect, new Color(30, 30, 30, 160));
        var hpos = new Vector2(panelRect.X + (panelRect.Width - hsz.X) / 2f,
            hintRect.Y + (hintRect.Height - hsz.Y) / 2f);
        sb.DrawStringSafe(UIAssets.DefaultFont, hint, hpos, Color.Gray);
    }

    private void DrawRightPane(SpriteBatch sb, Rectangle pane)
    {
        // Background
        sb.Draw(UIAssets.PixelTexture, pane, Color.Black * 0.25f);

        // Tabs bar at top
        var tabsRect = new Rectangle(pane.X + 6, pane.Y + 6, pane.Width - 12, 36);
        sb.Draw(UIAssets.PixelTexture, tabsRect, new Color(60, 60, 90, 100));

        var tabNames = new[] { "Items", "Equipment", "Key Items" };
        for (var i = 0; i < 3; i++)
        {
            var isActive = (int)_tab == i;
            var w = tabsRect.Width / 3;
            var r = new Rectangle(tabsRect.X + i * w, tabsRect.Y, w, tabsRect.Height);

            if (isActive) sb.Draw(UIAssets.PixelTexture, r, new Color(90, 90, 120, 160));
            var name = tabNames[i];
            var sz = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, name);
            var pos = new Vector2(r.X + (r.Width - sz.X) / 2f, r.Y + (r.Height - sz.Y) / 2f);
            sb.DrawStringSafe(UIAssets.DefaultFont, name, pos, isActive ? Color.White : Color.LightGray);
        }

        // List area (between tabs and description)
        var descHeight = 110;
        var listArea = new Rectangle(pane.X + 10, tabsRect.Bottom + 10, pane.Width - 20,
            pane.Height - (tabsRect.Height + 10) - descHeight - 16);
        sb.Draw(UIAssets.PixelTexture, listArea, new Color(0, 0, 0, 40));

        // Update viewport info for selection visibility
        _listViewTop = listArea.Y + 6;
        _listViewBottom = listArea.Bottom - 8;
        _listContentTop = _listViewTop;

        // Live stacks from global inventory
        var stacks = GetCurrentStacks();
        var selIdx = _sel[_tab];

        var y = listArea.Y + 6 - _listScroll;
        float leftPad = listArea.X + 10;

        // Auto-scroll to keep selection visible
        if (stacks.Count > 0)
        {
            var selTop = _listContentTop + selIdx * RowHeight;
            var selBottom = selTop + RowHeight;
            if (selBottom - _listScroll > _listViewBottom) _listScroll = selBottom - _listViewBottom;
            if (selTop - _listScroll < _listViewTop) _listScroll = selTop - _listViewTop;
            if (_listScroll < 0f) _listScroll = 0f;
        }

        for (var i = 0; i < stacks.Count; i++)
        {
            var (stack, def) = stacks[i];
            var selected = i == selIdx;

            var rowRect = new Rectangle(listArea.X + 2, (int)(y - 2), listArea.Width - 4, (int)(RowHeight + 4));
            if (rowRect.Bottom >= listArea.Top && rowRect.Top <= listArea.Bottom)
            {
                if (selected)
                {
                    sb.Draw(UIAssets.PixelTexture, rowRect, new Color(90, 90, 120, 140));
                    var accent = new Rectangle(rowRect.X, rowRect.Y, 3, rowRect.Height);
                    sb.Draw(UIAssets.PixelTexture, accent, Color.CornflowerBlue);
                }

                var text = stack.Quantity > 1 ? $"{def.Name} x{stack.Quantity}" : def.Name;
                var sz = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, text);
                var pos = new Vector2(leftPad, y + (RowHeight - sz.Y) / 2f);
                sb.DrawStringSafe(UIAssets.DefaultFont, text, pos, selected ? Color.White : Color.LightGray);
            }

            y += RowHeight;
        }

        // Description box at bottom
        var descRect = new Rectangle(pane.X + 10, pane.Bottom - descHeight - 8, pane.Width - 20, descHeight);
        sb.Draw(UIAssets.PixelTexture, descRect, new Color(0, 0, 0, 60));
        DrawBorder(sb, descRect, 2, new Color(255, 255, 255, 20));

        if (stacks.Count > 0)
        {
            var (stack, def) = stacks[selIdx];
            var title = def.Name;
            var tSz = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, title);
            var tPos = new Vector2(descRect.X + 8, descRect.Y + 8);
            sb.DrawStringSafe(UIAssets.DefaultFont, title, tPos, Color.White);

            var bodyRect = new Rectangle(descRect.X + 8, (int)(tPos.Y + tSz.Y + 6), descRect.Width - 16,
                descRect.Height - (int)(tSz.Y + 16));
            DrawWrap(sb, def.Description ?? "-", bodyRect, Color.LightGray);
        }
    }

    private void EnsureSelectionVisible()
    {
        var stacks = GetCurrentStacks();
        if (stacks.Count == 0)
        {
            _sel[_tab] = 0;
            _listScroll = 0f;
            return;
        }

        // Clamp selection into range in case inventory changed
        if (_sel[_tab] >= stacks.Count) _sel[_tab] = stacks.Count - 1;
        if (_sel[_tab] < 0) _sel[_tab] = 0;

        var selTop = _listContentTop + _sel[_tab] * RowHeight;
        var selBottom = selTop + RowHeight;

        if (_listViewBottom > _listViewTop)
        {
            if (selBottom - _listScroll > _listViewBottom)
                _listScroll = selBottom - _listViewBottom;

            if (selTop - _listScroll < _listViewTop)
                _listScroll = selTop - _listViewTop;

            if (_listScroll < 0f) _listScroll = 0f;
        }
    }

    private IReadOnlyList<(InventoryStack stack, InventoryItemDef def)> GetCurrentStacks()
    {
        var cat = _tab switch
        {
            InvTab.Items => ItemCategory.Items,
            InvTab.Equipment => ItemCategory.Equipment,
            InvTab.KeyItems => ItemCategory.KeyItems,
            _ => ItemCategory.Items
        };
        return Globals.Inventory.GetStacks(cat);
    }

    private void BackToGameMenu()
    {
        Globals.UI.Remove(this);
        Globals.UI.Add(new GameMenu());
    }

    private void UseSelected()
    {
        var stacks = GetCurrentStacks();
        if (stacks.Count == 0) return;

        var (stack, def) = stacks[_sel[_tab]];
        var handled = Globals.Inventory.Use(def.Id);

        var msg = new MessageBox(
            _tab switch
            {
                InvTab.Items => handled ? $"Used {def.Name}!" : $"{def.Name} can't be used.",
                InvTab.Equipment => $"Equipped {def.Name}!",
                InvTab.KeyItems => $"You inspect {def.Name}.",
                _ => def.Name
            }
        );
        Globals.UI.Add(msg);

        // After using, ensure selection is still valid if a stack was consumed
        EnsureSelectionVisible();
    }

    private void SortCurrent()
    {
        // GetStacks already returns items ordered by name; just reset selection/scroll.
        _sel[_tab] = 0;
        _listScroll = 0f;
    }

    // Helpers for drawing (styled to match GameMenu)
    private static void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c)
    {
        // Top/Bottom
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
        // Left/Right
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
    }

    private static void DrawSeparator(SpriteBatch sb, float left, float right, float y)
    {
        var rect = new Rectangle((int)left, (int)y, (int)(right - left), 1);
        sb.Draw(UIAssets.PixelTexture, rect, new Color(255, 255, 255, 30));
    }

    private static void DrawWrap(SpriteBatch sb, string text, Rectangle area, Color color)
    {
        var words = text.Split(' ');
        float x = area.X, y = area.Y;
        var line = "";
        foreach (var w in words)
        {
            var test = string.IsNullOrEmpty(line) ? w : line + " " + w;
            var size = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, test);
            if (size.X > area.Width)
            {
                sb.DrawStringSafe(UIAssets.DefaultFont, line, new Vector2(x, y), color);
                y += size.Y + 2;
                line = w;
                if (y > area.Bottom - size.Y) break;
            }
            else
            {
                line = test;
            }
        }

        if (!string.IsNullOrEmpty(line) && y <= area.Bottom)
            sb.DrawStringSafe(UIAssets.DefaultFont, line, new Vector2(x, y), color);
    }

    private enum InvTab
    {
        Items,
        Equipment,
        KeyItems
    }
}