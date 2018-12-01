using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Wobble;
using Wobble.Assets;

namespace Quaver.Shared.Assets
{
    public static class TitleHelper
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static Texture2D Get(Title title)
        {
            // ReSharper disable once ArrangeMethodOrOperatorBody
            return AssetLoader.LoadTexture2D(GameBase.Game.Resources.Get($"Quaver.Resources/Textures/UI/Titles/title-{(int) title}.png"));
        }
    }
}