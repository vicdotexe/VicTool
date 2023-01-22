namespace VicTool.Main.Misc
{
    public static class DecimalExt
    {
        public static decimal Round(this decimal value, int decimalPlace)
        {
            return decimal.Round(value, decimalPlace);
        }

        public static bool IsEqual(decimal a, decimal b, int decimalPlaces = 28)
        {
            return a.Round(decimalPlaces) == b.Round(decimalPlaces);
        }
    }
}
