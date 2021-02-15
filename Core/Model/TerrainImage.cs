using System.Collections.Generic;

namespace Core.Model
{
    public class TerrainImage
    {
        public int id;

        public string name;

        public List<TileAnimationFrame> animationDetail;
        public List<TileAnimationFrame> animationDetailSpring;
        public List<TileAnimationFrame> animationDetailSummer;
        public List<TileAnimationFrame> animationDetailAutumn;
        public List<TileAnimationFrame> animationDetailWinter;
        public List<TileAnimationFrame> animationDetailSnow;

        public List<TileAnimationFrame> animationView;
        public List<TileAnimationFrame> animationViewSpring;
        public List<TileAnimationFrame> animationViewSummer;
        public List<TileAnimationFrame> animationViewAutumn;
        public List<TileAnimationFrame> animationViewWinter;
        public List<TileAnimationFrame> animationViewSnow;
    }
}
