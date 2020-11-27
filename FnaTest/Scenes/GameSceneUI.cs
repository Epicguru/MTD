using MTD.Resources;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Nez.UI;

namespace MTD.Scenes
{
    public static class GameSceneUI
    {
        public static UICanvas Setup(Scene scene, SpriteAtlas atlas)
        {
            var ui = new UICanvas();
            ui.SetRenderLayer(Main.LAYER_UI);
            ui.IsFullScreen = true;

            var skin = Skin.CreateDefaultSkin();
            var font = Main.Font32;
            skin.Get<LabelStyle>().Font = font;
            skin.Get<TextButtonStyle>().Font = font;
            skin.Get<WindowStyle>().TitleFont = font;

            CreateResourceView(skin, ui.Stage);

            return ui;
        }

        private static void CreateResourceView(Skin skin, Stage stage)
        {
            var table = new Table().SetFillParent(true).Left().Top().PadLeft(10).PadTop(10);

            Label AddResource(string name, Sprite icon, int count)
            {
                var img = new Image(icon);
                table.Add(img).Size(32, 32).SetPadRight(5);
                var label = new Label($"{name}: {count}", skin);
                table.Add(label).SetAlign(Align.Left);
                table.Row().SetPadTop(5);
                return label;
            }

            foreach (var res in ResourceDef.All)
            {
                AddResource(res.Name, res.Icon, 0);
            }

            stage.AddElement(table);
        }
    }
}
