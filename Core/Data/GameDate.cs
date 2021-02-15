using System;

namespace Core.Data
{
    public class GameDate
    {
        public TimeSpan timeSpan = TimeSpan.Zero;

        public int year => timeSpan.Days / 360 + 1;
        public int month => timeSpan.Days / 30 + 1;
        public int day => timeSpan.Days % 30 + 1;

        public Season season
        {
            get
            {
                return month switch
                {
                    3 or 4 or 5 => Season.spring,
                    6 or 7 or 8 => Season.summer,
                    9 or 10 or 11 => Season.autumn,
                    12 or 1 or 2 => Season.winter,
                    _ => throw new ApplicationException(nameof(season)),
                };
            }
        }

        public GameDate addDay(int day = 1)
        {
            timeSpan += TimeSpan.FromDays(day);

            return this;
        }

        public enum Season
        {
            spring = 0,
            summer = 1,
            autumn = 2,
            winter = 3
        }
    }
}
