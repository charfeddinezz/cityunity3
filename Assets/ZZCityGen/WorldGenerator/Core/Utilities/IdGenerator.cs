using System;

namespace ZZCityGen.WorldGenerator.Core.Utilities
{
    public static class IdGenerator
    {
        private static int counter = Environment.TickCount & 0x00FFFFFF;

        public static string Next(string prefix = "id")
        {
            unchecked
            {
                counter++;
                return $"{prefix}_{counter:X8}";
            }
        }
    }
}