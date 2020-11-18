using Nez;
using Nez.Sprites;
using Nez.UI;

namespace MTD.Scenes
{
    public static class GameSceneUI
    {
        public static UICanvas Setup(Scene scene, SpriteAtlas atlas)
        {
            var ui = new UICanvas();
            ui.SetRenderLayer(999);
            ui.IsFullScreen = true;

            var skin = Skin.CreateDefaultSkin();
            var font = Main.Font32;
            skin.Get<LabelStyle>().Font = font;
            skin.Get<TextButtonStyle>().Font = font;
            skin.Get<WindowStyle>().TitleFont = font;

            CreateResourceView(skin, ui.Stage, atlas);

            return ui;
        }

        private static void CreateResourceView(Skin skin, Stage stage, SpriteAtlas atlas)
        {
            var table = new Table().SetFillParent(true).Left().Top().PadLeft(10).PadTop(10);

            var woodImg = new Image(atlas.GetSprite("Icons/Wood"));
            woodImg.SetSizeImg(32, 32);
            table.Add(woodImg).SetPadRight(5);
            table.Add(new Label($"Wood: {123}", skin)).SetAlign(Align.Left);
            table.Row().SetPadTop(5);

            var stoneImg = new Image(atlas.GetSprite("Icons/Stone"));
            stoneImg.SetSizeImg(32, 32);
            table.Add(stoneImg).SetPadRight(5);
            table.Add(new Label($"Stone: {545}", skin)).SetAlign(Align.Left);
            table.Row().SetPadTop(5);

            var ironImg = new Image(atlas.GetSprite("Icons/Iron"));
            ironImg.SetSizeImg(32, 32);
            table.Add(ironImg).SetPadRight(5);
            table.Add(new Label($"Iron: {98}", skin)).SetAlign(Align.Left);
            table.Row().SetPadTop(5);

            var goldImg = new Image(atlas.GetSprite("Icons/Gold"));
            goldImg.SetSizeImg(32, 32);
            table.Add(goldImg).SetPadRight(5);
            table.Add(new Label($"Gold: {12}", skin)).SetAlign(Align.Left);
            table.Row().SetPadTop(5);

            var cobImg = new Image(atlas.GetSprite("Icons/Cobalt"));
            cobImg.SetSizeImg(32, 32);
            table.Add(cobImg).SetPadRight(5);
            table.Add(new Label($"Cobalt: {49}", skin)).SetAlign(Align.Left);
            table.Row().SetPadTop(5);

            stage.AddElement(table);
        }
    }
}
