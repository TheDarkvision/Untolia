using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Untolia.Core.RPG;

namespace Untolia.Core.UI;

// A modal menu showing active party on the left and detailed stats on the right.
// Navigate members with Up/Down. ESC to go back.
// Supports vertical scrolling for the right pane (mouse wheel or PageUp/PageDown).
public sealed class StatsMenu : Menu
{
    private int _memberIndex = 0;

    // ... existing code ...
    private static readonly Dictionary<string, Texture2D> _portraitCache = new(StringComparer.OrdinalIgnoreCase);

    // Scroll state for the right pane
    private float _scrollOffset = 0f;
    private float _scrollVelocity = 0f;
    private int _prevWheel; // mouse wheel
    
    private const float ScrollSpeedWheel = 60f;    // pixels per wheel tick (scaled)
    private const float ScrollSpeedKeys = 2400f;    // pixels per second when using PageUp/Down
    private const float ScrollDamping = 8f;        // smooth damping for velocity

    public StatsMenu()
    {
        IsModal = true;
        CanReceiveFocus = true;
        BackgroundColor = Color.Black * 0.85f;
        BorderThickness = 3;

        PositionMenu();
    }

    public override void OnAdded()
    {
        base.OnAdded();
        _prevWheel = Mouse.GetState().ScrollWheelValue;
        _scrollOffset = 0f;
        _scrollVelocity = 0f;
    }

    private void PositionMenu()
    {
        // Wider layout: centered with margins
        var margin = 40f;
        var width = Globals.ScreenSize.X - margin * 2f;
        var height = Globals.ScreenSize.Y - margin * 2f;
        Size = new Vector2(width, height);
        Position = new Vector2(margin, margin);
    }

    protected override void CloseMenu()
    {
        Globals.UI.Remove(this);
        Globals.UI.Add(new GameMenu());
    }

    public override void Update(float deltaTime)
    {
        // Debounced selection (same as before)
        if (_navCooldown > 0f) _navCooldown -= deltaTime;
        if (_selectCooldown > 0f) _selectCooldown -= deltaTime;

        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();

        var up = !_previousKeyboard.IsKeyDown(Keys.Up) && keyboard.IsKeyDown(Keys.Up);
        var down = !_previousKeyboard.IsKeyDown(Keys.Down) && keyboard.IsKeyDown(Keys.Down);

        var active = Globals.Party.Active.ToList();
        if (active.Count == 0)
        {
            var esc = !_previousKeyboard.IsKeyDown(Keys.Escape) && keyboard.IsKeyDown(Keys.Escape);
            if (_selectCooldown <= 0f && esc) { CloseMenu(); _selectCooldown = 0.18f; }
            _previousKeyboard = keyboard;
            _prevWheel = mouse.ScrollWheelValue;
            return;
        }

        if (_navCooldown <= 0f && (up || down))
        {
            _memberIndex = (up)
                ? (_memberIndex - 1 + active.Count) % active.Count
                : (_memberIndex + 1) % active.Count;
            _navCooldown = 0.14f;

            // Reset scroll when switching member
            _scrollOffset = 0f;
            _scrollVelocity = 0f;
        }

        var escPressed = !_previousKeyboard.IsKeyDown(Keys.Escape) && keyboard.IsKeyDown(Keys.Escape);
        if (_selectCooldown <= 0f && escPressed)
        {
            CloseMenu();
            _selectCooldown = 0.18f;
        }

        // Handle scrolling (mouse wheel)
        int wheel = mouse.ScrollWheelValue;
        int deltaWheel = wheel - _prevWheel;
        _prevWheel = wheel;
        if (deltaWheel != 0)
        {
            // Typically wheel delta is 120 per notch
            _scrollVelocity -= (deltaWheel / 120f) * ScrollSpeedWheel;
        }

        // Handle keyboard scrolling
        bool pageUp = keyboard.IsKeyDown(Keys.PageUp);
        bool pageDown = keyboard.IsKeyDown(Keys.PageDown);
        if (pageUp) _scrollVelocity -= ScrollSpeedKeys * deltaTime;
        if (pageDown) _scrollVelocity += ScrollSpeedKeys * deltaTime;

        // Apply velocity with damping
        if (Math.Abs(_scrollVelocity) > 0.001f)
        {
            _scrollOffset += _scrollVelocity * deltaTime;
            _scrollVelocity = MathHelper.Lerp(_scrollVelocity, 0f, MathHelper.Clamp(ScrollDamping * deltaTime, 0f, 1f));
        }

        _previousKeyboard = keyboard;
    }

    protected override void DrawTitle(SpriteBatch spriteBatch)
    {
        var title = "PARTY STATUS";
        var size = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, title);
        var pos = new Vector2(Position.X + (Size.X - size.X) / 2f, Position.Y + 15f);
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, title, pos, Color.White);
    }

    private float DrawRightContent(SpriteBatch sb, PartyMember sel, Rectangle rightPane, bool measureOnly, float scroll)
    {
        float marginInner = 8f;
        float x = rightPane.X + marginInner;
        float y0 = rightPane.Y + marginInner;
        float y = y0 - scroll;
        float line = 26f;

        // Fancy header bar
        if (!measureOnly)
        {
            var headerRect = new Rectangle((int)x, (int)y, rightPane.Width - (int)(marginInner * 2), 30);
            if (IsVisibleIn(headerRect, rightPane))
                sb.Draw(UIAssets.PixelTexture, headerRect, Color.Black * 0.35f);

            var nameText = $"{sel.Name}  (Lv {sel.Progress.Level})";
            if (headerRect.Bottom > rightPane.Top && headerRect.Top < rightPane.Bottom)
                sb.DrawStringSafe(UIAssets.DefaultFont, nameText, new Vector2(x + 6, y + 6), Color.White);
        }
        y += line + 6;

        // Portrait + main vitals row
        var portraitRect = new Rectangle((int)x, (int)y, 192, 192);
        if (!measureOnly)
        {
            if (IsVisibleIn(portraitRect, rightPane)) DrawPortrait(sb, sel, portraitRect);

            // HP/MP/EXP to the right of portrait
            float textX = portraitRect.Right + 16;
            float textY = y + 4;

            var hpRectY = new Vector2(textX, textY);
            if (hpRectY.Y + 18 > rightPane.Top && hpRectY.Y < rightPane.Bottom)
                DrawVital(sb, "HP", sel.HP.Current, sel.HP.Max, hpRectY, rightPane.Width - (portraitRect.Width + 200), new Color(200, 60, 60));
            textY += line;

            var mpRectY = new Vector2(textX, textY);
            if (mpRectY.Y + 18 > rightPane.Top && mpRectY.Y < rightPane.Bottom)
                DrawVital(sb, "MP", sel.MP.Current, sel.MP.Max, mpRectY, rightPane.Width - (portraitRect.Width + 200), new Color(60, 120, 200));
            textY += line * 1.2f;

            var expToNext = sel.Progress.ExpToNext;
            var exp = sel.Progress.Exp;
            var expRectY = new Vector2(textX, textY);
            if (expRectY.Y + 18 > rightPane.Top && expRectY.Y < rightPane.Bottom)
                DrawVital(sb, "EXP", exp, expToNext, expRectY, rightPane.Width - (portraitRect.Width + 200), new Color(220, 180, 60));
        }
        y += 192 + 16;

        // Two columns
        float col1X = x;
        float col1Y = y;
        float col2X = x + 260f;
        float row = 24f;

        // Column 1: Primary stats
        if (!measureOnly)
        {
            DrawSectionHeader(sb, "Attributes", new Vector2(col1X, col1Y), rightPane);
        }
        col1Y += row;
        if (!measureOnly && IsLineVisible(col1Y, rightPane)) DrawLabelValue(sb, "Force", sel.BaseStats.Force.ToString(), new Vector2(col1X, col1Y));
        col1Y += row;
        if (!measureOnly && IsLineVisible(col1Y, rightPane)) DrawLabelValue(sb, "Focus", sel.BaseStats.Focus.ToString(), new Vector2(col1X, col1Y));
        col1Y += row;
        if (!measureOnly && IsLineVisible(col1Y, rightPane)) DrawLabelValue(sb, "Resolve", sel.BaseStats.Resolve.ToString(), new Vector2(col1X, col1Y));
        col1Y += row;
        if (!measureOnly && IsLineVisible(col1Y, rightPane)) DrawLabelValue(sb, "Agility", sel.BaseStats.Agility.ToString(), new Vector2(col1X, col1Y));
        col1Y += row * 1.2f;

        // Column 2: Equipment + Affinities + Tags
        float infoY = y;
        if (!measureOnly) DrawSectionHeader(sb, "Equipment", new Vector2(col2X, infoY), rightPane);
        infoY += row;
        if (!measureOnly && IsLineVisible(infoY, rightPane)) DrawLabelValue(sb, "Weapon", sel.WeaponId ?? "-", new Vector2(col2X, infoY));
        infoY += row;
        if (!measureOnly && IsLineVisible(infoY, rightPane)) DrawLabelValue(sb, "Shield", sel.ShieldId ?? "-", new Vector2(col2X, infoY));
        infoY += row;
        if (!measureOnly && IsLineVisible(infoY, rightPane)) DrawLabelValue(sb, "Armor", sel.ArmorId ?? "-", new Vector2(col2X, infoY));
        infoY += row;
        if (!measureOnly && IsLineVisible(infoY, rightPane)) DrawLabelValue(sb, "Trinket", sel.TrinketId ?? "-", new Vector2(col2X, infoY));
        infoY += row * 1.2f;

        if (!measureOnly) DrawSectionHeader(sb, "Affinities", new Vector2(col2X, infoY), rightPane);
        infoY += row;
        if (!measureOnly && IsLineVisible(infoY, rightPane)) DrawLabelValue(sb, "Resist", (sel.Resist.Count == 0 ? "-" : string.Join(", ", sel.Resist)), new Vector2(col2X, infoY));
        infoY += row;
        if (!measureOnly && IsLineVisible(infoY, rightPane)) DrawLabelValue(sb, "Weak", (sel.Weak.Count == 0 ? "-" : string.Join(", ", sel.Weak)), new Vector2(col2X, infoY));
        infoY += row;
        if (!measureOnly && IsLineVisible(infoY, rightPane)) DrawLabelValue(sb, "Immune", (sel.Immune.Count == 0 ? "-" : string.Join(", ", sel.Immune)), new Vector2(col2X, infoY));
        infoY += row * 1.2f;

        if (!measureOnly) DrawSectionHeader(sb, "Tags", new Vector2(col2X, infoY), rightPane);
        infoY += row;
        if (!measureOnly)
        {
            var tagsText = (sel.Tags.Count == 0 ? "-" : string.Join(", ", sel.Tags));
            DrawWrap(sb, tagsText, new Rectangle((int)col2X, (int)infoY, rightPane.Right - (int)col2X - 16, (int)row * 2), Color.White, rightPane);
        }
        infoY += row * 2f;

        // Extended bio from CharacterDef (appearance, lore, role)
        var def = Globals.Characters.Get(sel.Id);
        float bioY = Math.Max(col1Y, infoY) + 10;

        if (!measureOnly) DrawSectionHeader(sb, "Appearance", new Vector2(x, bioY), rightPane);
        bioY += row;
        if (def?.Appearance != null)
        {
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Age", def.Appearance.Age ?? "-", new Vector2(x, bioY)); bioY += row;
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Build", def.Appearance.Build ?? "-", new Vector2(x, bioY)); bioY += row;
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Hair", def.Appearance.Hair ?? "-", new Vector2(x, bioY)); bioY += row;
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Eyes", def.Appearance.Eyes ?? "-", new Vector2(x, bioY)); bioY += row;
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Skin", def.Appearance.Skin ?? "-", new Vector2(x, bioY)); bioY += row;
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Attire", def.Appearance.Attire ?? "-", new Vector2(x, bioY)); bioY += row * 1.2f;
        }

        if (!measureOnly) DrawSectionHeader(sb, "Lore", new Vector2(x, bioY), rightPane);
        bioY += row;
        if (def?.Lore != null)
        {
            if (!measureOnly) DrawWrap(sb, def.Lore.Origin ?? "-", new Rectangle((int)x, (int)bioY, rightPane.Width - 32, (int)(row * 1.6f)), Color.White, rightPane);
            bioY += row * 1.8f;
            if (!measureOnly) DrawWrap(sb, def.Lore.Motivation ?? "-", new Rectangle((int)x, (int)bioY, rightPane.Width - 32, (int)(row * 2.2f)), Color.White, rightPane);
            bioY += row * 2.4f;
            if (!measureOnly) DrawWrap(sb, def.Lore.Symbol ?? "-", new Rectangle((int)x, (int)bioY, rightPane.Width - 32, (int)(row * 1.6f)), Color.White, rightPane);
            bioY += row * 1.8f;
        }

        if (!measureOnly) DrawSectionHeader(sb, "Role", new Vector2(x, bioY), rightPane);
        bioY += row;
        if (def?.Role != null)
        {
            if (!measureOnly) DrawWrap(sb, def.Role.BattleStyle ?? "-", new Rectangle((int)x, (int)bioY, rightPane.Width - 32, (int)(row * 1.8f)), Color.White, rightPane);
            bioY += row * 2.0f;
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Strengths", def.Role.Strengths is { Count: > 0 } ? string.Join(", ", def.Role.Strengths) : "-", new Vector2(x, bioY)); bioY += row;
            if (!measureOnly && IsLineVisible(bioY, rightPane)) DrawLabelValue(sb, "Weaknesses", def.Role.Weaknesses is { Count: > 0 } ? string.Join(", ", def.Role.Weaknesses) : "-", new Vector2(x, bioY)); bioY += row;
        }

        // content height = last y drawn relative to y0
        float contentHeight = (bioY - (y0 - scroll)) + marginInner;
        return contentHeight;
    }

    private void DrawScrollBar(SpriteBatch sb, Rectangle rightPane, float scroll, float contentHeight, float visibleHeight)
    {
        var trackWidth = 8;
        var trackRect = new Rectangle(rightPane.Right - trackWidth - 4, rightPane.Y + 6, trackWidth, rightPane.Height - 12);

        sb.Draw(UIAssets.PixelTexture, trackRect, new Color(255, 255, 255, 30));

        float ratio = Math.Max(visibleHeight / contentHeight, 0.1f);
        int thumbH = (int)(trackRect.Height * ratio);
        int maxThumbTravel = trackRect.Height - thumbH;
        int thumbY = trackRect.Y + (contentHeight <= 0 ? 0 : (int)(MathHelper.Clamp(scroll / (contentHeight - visibleHeight), 0f, 1f) * maxThumbTravel));

        var thumbRect = new Rectangle(trackRect.X, thumbY, trackRect.Width, thumbH);
        sb.Draw(UIAssets.PixelTexture, thumbRect, new Color(180, 180, 180, 180));
        // Outline
        sb.Draw(UIAssets.PixelTexture, new Rectangle(thumbRect.X, thumbRect.Y, thumbRect.Width, 1), new Color(0, 0, 0, 60));
        sb.Draw(UIAssets.PixelTexture, new Rectangle(thumbRect.X, thumbRect.Bottom - 1, thumbRect.Width, 1), new Color(0, 0, 0, 60));
    }

    private static bool IsVisibleIn(Rectangle item, Rectangle clip) =>
        item.Bottom >= clip.Top && item.Top <= clip.Bottom;

    private static bool IsLineVisible(float y, Rectangle clip) =>
        y >= clip.Top && y <= clip.Bottom;

    private void DrawSectionHeader(SpriteBatch sb, string title, Vector2 pos, Rectangle clip)
    {
        var barRect = new Rectangle((int)pos.X, (int)pos.Y, clip.Width - 16, 20);
        if (IsVisibleIn(barRect, clip))
        {
            sb.Draw(UIAssets.PixelTexture, barRect, new Color(60, 60, 90, 120));
            sb.DrawStringSafe(UIAssets.DefaultFont, title, new Vector2(pos.X + 4, pos.Y + 2), Color.CornflowerBlue);
        }
    }

    private void DrawHeader(SpriteBatch sb, string text, Vector2 pos)
    {
        sb.DrawStringSafe(UIAssets.DefaultFont, text, pos, Color.CornflowerBlue);
    }

    private void DrawLabelValue(SpriteBatch sb, string label, string value, Vector2 pos)
    {
        sb.DrawStringSafe(UIAssets.DefaultFont, $"{label}:", pos, Color.LightGray);
        var labW = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, $"{label}: ").X;
        sb.DrawStringSafe(UIAssets.DefaultFont, value, new Vector2(pos.X + labW + 4, pos.Y), Color.White);
    }

    private void DrawWrap(SpriteBatch sb, string text, Rectangle area, Color color, Rectangle clip)
    {
        // Greedy word wrap with visibility checks
        var words = text.Split(' ');
        float x = area.X, y = area.Y;
        string line = "";
        foreach (var w in words)
        {
            var test = string.IsNullOrEmpty(line) ? w : line + " " + w;
            var size = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, test);
            if (size.X > area.Width)
            {
                if (y + size.Y > clip.Top && y < clip.Bottom)
                    sb.DrawStringSafe(UIAssets.DefaultFont, line, new Vector2(x, y), color);
                y += size.Y + 2;
                line = w;
                if (y > area.Bottom) break;
            }
            else
            {
                line = test;
            }
        }
        if (!string.IsNullOrEmpty(line) && y <= area.Bottom)
        {
            if (y + UIAssets.MeasureStringSafe(UIAssets.DefaultFont, line).Y > clip.Top)
                sb.DrawStringSafe(UIAssets.DefaultFont, line, new Vector2(x, y), color);
        }
    }

    private void DrawPortrait(SpriteBatch sb, PartyMember member, Rectangle rect)
    {
        var tex = LoadPortrait(member.Portrait);
        if (tex != null)
        {
            var dest = FitRect(tex.Width, tex.Height, rect);
            if (IsVisibleIn(dest, new Rectangle(rect.X, rect.Y - 99999, rect.Width, rect.Height + 99999)))
                sb.Draw(tex, dest, Color.White);

            // Fancy frame
            sb.Draw(UIAssets.PixelTexture, new Rectangle(dest.X - 2, dest.Y - 2, dest.Width + 4, 2), new Color(255, 255, 255, 60));
            sb.Draw(UIAssets.PixelTexture, new Rectangle(dest.X - 2, dest.Bottom, dest.Width + 4, 2), new Color(0, 0, 0, 80));
            sb.Draw(UIAssets.PixelTexture, new Rectangle(dest.X - 2, dest.Y - 2, 2, dest.Height + 4), new Color(255, 255, 255, 60));
            sb.Draw(UIAssets.PixelTexture, new Rectangle(dest.Right, dest.Y - 2, 2, dest.Height + 4), new Color(0, 0, 0, 80));
        }
        else
        {
            sb.Draw(UIAssets.PixelTexture, rect, Color.DimGray * 0.6f);
            var msg = "No Portrait";
            var sz = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, msg);
            sb.DrawStringSafe(UIAssets.DefaultFont, msg,
                new Vector2(rect.X + (rect.Width - sz.X) / 2f, rect.Y + (rect.Height - sz.Y) / 2f),
                Color.White);
        }
    }

    // Keep a copy of the current right pane for clipping helpers
    private Rectangle _currentRightPane;

    public override void Draw(SpriteBatch spriteBatch)
    {
        DrawBackground(spriteBatch);
        DrawTitle(spriteBatch);

        var margin = 24f;
        var contentTop = Position.Y + 50f;
        var leftPaneWidth = 260f;

        var leftPane = new Rectangle((int)(Position.X + margin), (int)contentTop, (int)leftPaneWidth, (int)(Size.Y - contentTop + Position.Y - margin));
        var rightPane = new Rectangle((int)(leftPane.Right + margin), (int)contentTop, (int)(Size.X - leftPaneWidth - margin * 3f), (int)(Size.Y - contentTop + Position.Y - margin));
        _currentRightPane = rightPane;

        
        // Left pane background
        spriteBatch.Draw(UIAssets.PixelTexture, leftPane, Color.Black * 0.3f);

        // List active party
        var active = Globals.Party.Active.ToList();
        if (active.Count == 0)
        {
            spriteBatch.DrawStringSafe(UIAssets.DefaultFont, "No active party members.", new Vector2(leftPane.X + 10, leftPane.Y + 10), Color.Gray);
            DrawInstructions(spriteBatch); // keep global footer when no data
            return;
        }


       if (_memberIndex >= active.Count) _memberIndex = active.Count - 1;

        float itemY = leftPane.Y + 10;
        for (int i = 0; i < active.Count; i++)
        {
            var m = active[i];
            var color = (i == _memberIndex) ? Color.Yellow : Color.White;
            var label = $"{m.Name}  Lv {m.Progress.Level}";
            spriteBatch.DrawStringSafe(UIAssets.DefaultFont, label, new Vector2(leftPane.X + 12, itemY), color);
            itemY += 28f;
        }

        // Right pane background
        spriteBatch.Draw(UIAssets.PixelTexture, rightPane, Color.Black * 0.2f);

        // Draw scrollable content in rightPane
        var sel = active[_memberIndex];

        // Measure, clamp, draw
        float contentHeight = DrawRightContent(spriteBatch, sel, rightPane, measureOnly: true, scroll: 0f);
        float visibleHeight = rightPane.Height - 8f;
        float maxScroll = Math.Max(0f, contentHeight - visibleHeight);
        if (maxScroll <= 0f) _scrollOffset = 0f;
        _scrollOffset = MathHelper.Clamp(_scrollOffset, 0f, maxScroll);
        DrawRightContent(spriteBatch, sel, rightPane, measureOnly: false, scroll: _scrollOffset);

        if (maxScroll > 1f)
            DrawScrollBar(spriteBatch, rightPane, _scrollOffset, contentHeight, visibleHeight);

        // Pane-local footer (clipped and truncated to the pane)
        DrawPaneInstructions(spriteBatch, rightPane);

    }

// New: draw pane-local instructions text, hidden/truncated so it never overflows the right pane
    private void DrawPaneInstructions(SpriteBatch sb, Rectangle pane)
    {
        var hint = "UP/DOWN: Select   ESC: Back   PgUp/PgDn or Mouse Wheel: Scroll";
        var size = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, hint);

        // Horizontal margin inside pane
        int innerPad = 8;
        int maxWidth = Math.Max(0, pane.Width - innerPad * 2);

        // If the hint is wider than the pane, truncate with ellipsis
        if (size.X > maxWidth)
            hint = TruncateWithEllipsis(hint, maxWidth);

        // Draw within pane bottom, aligned to center inside pane width
        var hintSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, hint);
        float x = pane.X + innerPad + (maxWidth - hintSize.X) / 2f;
        float y = pane.Bottom - hintSize.Y - innerPad;

        // Clip vertically: only draw if within pane
        if (y + hintSize.Y > pane.Top && y < pane.Bottom)
            sb.DrawStringSafe(UIAssets.DefaultFont, hint, new Vector2(x, y), Color.Gray);
    }
    
    private static string TruncateWithEllipsis(string text, int maxPixels)
    {
        // Early out
        if (UIAssets.MeasureStringSafe(UIAssets.DefaultFont, text).X <= maxPixels)
            return text;

        const string ellipsis = "...";
        int low = 0, high = text.Length;
        // Binary search max chars that fit with ellipsis
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            string candidate = text[..mid] + ellipsis;
            if (UIAssets.MeasureStringSafe(UIAssets.DefaultFont, candidate).X <= maxPixels)
                low = mid;
            else
                high = mid - 1;
        }
        return (low <= 0) ? ellipsis : text[..low] + ellipsis;
    }

    
    // Draw a texture clipped to a rectangular clip area by computing a matching source rectangle.
    private static void DrawTextureClipped(SpriteBatch sb, Texture2D tex, Rectangle dest, Rectangle clip)
    {
        var inter = Rectangle.Intersect(dest, clip);
        if (inter.Width <= 0 || inter.Height <= 0) return;

        // Map intersected dest back to source pixels
        float sX = (inter.X - dest.X) / (float)dest.Width;
        float sY = (inter.Y - dest.Y) / (float)dest.Height;
        float sW = inter.Width / (float)dest.Width;
        float sH = inter.Height / (float)dest.Height;

        var src = new Rectangle(
            (int)(sX * tex.Width),
            (int)(sY * tex.Height),
            Math.Max(1, (int)(sW * tex.Width)),
            Math.Max(1, (int)(sH * tex.Height))
        );

        sb.Draw(tex, inter, src, Color.White);
    }

    // Draw a solid rectangle clipped to clip area
    private static void DrawClippedRect(SpriteBatch sb, Rectangle rect, Color color, Rectangle clip)
    {
        var inter = Rectangle.Intersect(rect, clip);
        if (inter.Width <= 0 || inter.Height <= 0) return;
        sb.Draw(UIAssets.PixelTexture, inter, color);
    }

    private static Rectangle FitRect(int srcW, int srcH, Rectangle target)
    {
        if (srcW <= 0 || srcH <= 0) return target;
        float sx = target.Width / (float)srcW;
        float sy = target.Height / (float)srcH;
        float s = Math.Min(sx, sy);
        int w = (int)(srcW * s);
        int h = (int)(srcH * s);
        int x = target.X + (target.Width - w) / 2;
        int y = target.Y + (target.Height - h) / 2;
        return new Rectangle(x, y, w, h);
    }

    private static Texture2D? LoadPortrait(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        // Normalize path separators; MonoGame Content pipeline is case-sensitive on Linux
        var key = path.Replace('\\', '/');

        // Serve from cache if available
        if (_portraitCache.TryGetValue(key, out var cached))
            return cached;

        // 1) Try ContentManager (processed content, .xnb), prefer path without extension
        //    Requires that Portraits/* are included in Content.mgcb and built.
        try
        {
            string assetName = System.IO.Path.ChangeExtension(key, null) ?? key; // strip .png if present
            if (Globals.Content != null)
            {
                var tex = Globals.Content.Load<Texture2D>(assetName);
                _portraitCache[key] = tex;
                return tex;
            }
        }
        catch
        {
            // ignore and try raw file
        }

        // 2) Try raw file under bin/.../Content (Copy to Output)
        //    Works if you set Portraits/*.png to "Copy to Output Directory".
        try
        {
            var full = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content", key);
            if (System.IO.File.Exists(full))
            {
                using var fs = System.IO.File.OpenRead(full);
                var tex = Texture2D.FromStream(Globals.GraphicsDevice, fs);
                _portraitCache[key] = tex;
                return tex;
            }

            // If no extension was provided, try .png
            if (!System.IO.Path.HasExtension(key))
            {
                var fullPng = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content", key + ".png");
                if (System.IO.File.Exists(fullPng))
                {
                    using var fs = System.IO.File.OpenRead(fullPng);
                    var tex = Texture2D.FromStream(Globals.GraphicsDevice, fs);
                    _portraitCache[key] = tex;
                    return tex;
                }
            }
        }
        catch
        {
            // ignore
        }

        // 3) As a convenience on Linux/mac (case-sensitive FS), try a lowercase folder name fallback
        try
        {
            var lowered = key;
            var firstSlash = key.IndexOf('/');
            if (firstSlash > 0)
            {
                var folder = key.Substring(0, firstSlash);
                var rest = key.Substring(firstSlash + 1);
                lowered = folder.ToLowerInvariant() + "/" + rest;
            }

            string[] candidates =
            {
                System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content", lowered),
                System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content", lowered + ".png")
            };

            foreach (var cand in candidates)
            {
                if (System.IO.File.Exists(cand))
                {
                    using var fs = System.IO.File.OpenRead(cand);
                    var tex = Texture2D.FromStream(Globals.GraphicsDevice, fs);
                    _portraitCache[key] = tex;
                    return tex;
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    
    private void DrawInstructions(SpriteBatch spriteBatch)
    {
        // Add scrolling hints as well
        var hint = "UP/DOWN: Select member   ESC: Back   PgUp/PgDn or Mouse Wheel: Scroll";
        var size = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, hint);
        var pos = new Vector2(Position.X + (Size.X - size.X) / 2f, Position.Y + Size.Y - 28f);
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, hint, pos, Color.Gray);
    }

    private static void DrawVital(SpriteBatch sb, string label, int current, int max, Vector2 pos, int width, Color barColor)
    {
        // Text
        var text = $"{label}: {current} / {max}";
        sb.DrawStringSafe(UIAssets.DefaultFont, text, pos, Color.White);

        // Bar geometry
        var barX = pos.X + 160;
        var barY = pos.Y + 6;
        var barH = 12;

        // Track
        var backRect = new Rectangle((int)barX, (int)barY, Math.Max(1, width), barH);
        sb.Draw(UIAssets.PixelTexture, backRect, new Color(255, 255, 255, 30));

        // Fill
        float pct = max > 0 ? MathHelper.Clamp(current / (float)max, 0f, 1f) : 0f;
        var fillRect = new Rectangle((int)barX, (int)barY, (int)(width * pct), barH);
        if (fillRect.Width > 0)
            sb.Draw(UIAssets.PixelTexture, fillRect, barColor);

        // Simple border
        sb.Draw(UIAssets.PixelTexture, new Rectangle(backRect.X, backRect.Y, backRect.Width, 1), new Color(0, 0, 0, 40));
        sb.Draw(UIAssets.PixelTexture, new Rectangle(backRect.X, backRect.Bottom - 1, backRect.Width, 1), new Color(0, 0, 0, 40));
        sb.Draw(UIAssets.PixelTexture, new Rectangle(backRect.X, backRect.Y, 1, backRect.Height), new Color(0, 0, 0, 40));
        sb.Draw(UIAssets.PixelTexture, new Rectangle(backRect.Right - 1, backRect.Y, 1, backRect.Height), new Color(0, 0, 0, 40));
    }

    
}
