namespace Calculator.Models;

public class CalculationHistory
{
    public int Id { get; set; }
    public double Operand1 { get; set; }
    public double Operand2 { get; set; }
    public string Operation { get; set; } = string.Empty;
    public double? Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}