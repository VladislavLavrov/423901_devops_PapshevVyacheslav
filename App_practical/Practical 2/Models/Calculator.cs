namespace Calculator.Models;

public class Calculator
{

    public static double? Sum(double a, double b)
    {
        return a + b;
    }

    public static double? Minus(double a, double b)
    {

        return a - b;

    }

    public static double? Division(double a, double b)
    {
        try
        {
            if (b == 0)
            {
                throw new Exception("Zerro division");
            }

            return a / b;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
        
    }
    
    public static double Multiply(double a, double b)
    {
        return a * b;
    }

}
