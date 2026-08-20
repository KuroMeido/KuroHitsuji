namespace KuroHitsuji.Lib
{
    internal class UnitHelper
    {
        public static double DegreeToRadian(double degree)
        {
            return degree * Math.PI / 180;
        }
        public static double DegreeToRadian(double degree, int digits)
        {
            return Math.Round(degree * Math.PI / 180, digits);
        }
        public static double RadianToDegree(double radian)
        {
            return radian * 180 / Math.PI;
        }
        public static double RadianToDegree(double radian, int digits)
        {
            return Math.Round(radian * 180 / Math.PI, digits);
        }
        public static double FeetToMilimeter(double feet, int digits)
        {
            return Math.Round(feet * 304.8, digits);
        }
        public static double FeetToMilimeter(double feet)
        {
            return feet * 304.8;
        }
        public static double MilimeterToFeet(double mm)
        {
            return mm / 304.8;
        }
        public static double MilimeterToFeet(double mm, int digits)
        {
            return Math.Round(mm / 304.8, digits);
        }

        public static double Round8(double value)
        {
            return Math.Round(value, 8);
        }
        public static double Round0(double value)
        {
            return Math.Round(value, 0);
        }
        public static bool IsEqualNumber(double number1, double number2)
        {
            double mm1 = FeetToMilimeter(number1, 3);
            double mm2 = FeetToMilimeter(number2, 3);
            return mm1 == mm2;
        }
        public static bool IsZero(double feet)
        {
            double mm = FeetToMilimeter(feet, 3);
            return mm == 0;
        }
    }
}
