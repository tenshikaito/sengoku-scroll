using System.Collections.Generic;
using WinLibrary;

namespace Game.Graphic
{
    public class ZoomableTileMapSprites<T> : GameObject where T : TileMapSpritesBase
    {
        private List<T> tileMapSpritesList;
        private int tileMapIndex = 0;

        public T tileMapSprites => tileMapSpritesList[tileMapIndex];

        public ZoomableTileMapSprites() { }

        public ZoomableTileMapSprites(List<T> list)
        {
            setTileMap(list);

            previous();
        }

        public void setTileMap(List<T> list)
        {
            children.Clear();

            tileMapSpritesList = list;

            children.AddRange(list);

            if (tileMapIndex >= list.Count) tileMapIndex = list.Count - 1;

            hideAll();

            showCurrent();
        }

        public bool next()
        {
            hideAll();

            var count = tileMapSpritesList.Count;

            if (count == 0) return false;

            var flag = true;

            if (++tileMapIndex >= count)
            {
                tileMapIndex = count - 1;
                flag = false;
            }

            showCurrent();

            return flag;
        }

        public bool previous()
        {
            hideAll();

            if (tileMapSpritesList.Count == 0) return false;

            var flag = true;

            if (--tileMapIndex < 0)
            {
                tileMapIndex = 0;
                flag = false;
            }

            showCurrent();

            return flag;
        }

        private void showCurrent() => tileMapSpritesList[tileMapIndex].isEnable = true;

        private void hideAll() => tileMapSpritesList.ForEach(o => o.isEnable = false);
    }
}
