using System.Collections.Generic;
using PeakSentinel.Reporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class ShieldGlyph
    {
        private const float Size = 64f;
        private const float Baseline = 52f;
        private const float Advance = 68f;

        private readonly ShieldSprite _shield = new ShieldSprite();

        public void Register()
        {
            Texture texture = _shield.Sprite.texture;
            TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = FeedIcons.AssetName;
            asset.hideFlags = HideFlags.HideAndDontSave;
            asset.spriteSheet = texture;
            asset.material = new Material(Canvas.GetDefaultCanvasMaterial()) { mainTexture = texture, hideFlags = HideFlags.HideAndDontSave };
            asset.spriteInfoList = new List<TMP_Sprite>
            {
                new TMP_Sprite { name = FeedIcons.ShieldName, width = Size, height = Size, yOffset = Baseline, xAdvance = Advance, scale = 1f, sprite = _shield.Sprite },
            };
            asset.UpdateLookupTables(); // TMP builds the glyph tables from spriteInfoList on its first lookup (legacy upgrade path)
            MaterialReferenceManager.AddSpriteAsset(asset);
        }
    }
}
