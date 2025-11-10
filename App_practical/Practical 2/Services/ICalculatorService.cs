using Calculator.Models;

namespace Calculator.Services;

public interface ICalculatorService
{
    Task<CalculationResult> CalculateAsync(double a, double b, string operation);
    Task<List<CalculationHistory>> GetHistoryAsync();
    Task<CalculationHistory?> GetCalculationByIdAsync(int id);
}

public record CalculationResult(double? Result, bool Success, string? ErrorMessage = null);