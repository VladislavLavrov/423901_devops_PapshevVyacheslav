using Calculator.Models;
using Calculator.Data;

namespace Calculator.Services;

public interface ICalculatorService
{
    Task<CalculationResult> CalculateAsync(double a, double b, string operation);
    Task<List<CalculationHistory>> GetHistoryAsync();
    Task<CalculationHistory?> GetCalculationByIdAsync(int id);
    Task<bool> DeleteCalculationAsync(int id);
    Task<CalculationResult> UpdateCalculationAsync(int id, double operand1, double operand2, string operation);


}

public record CalculationResult(double? Result, bool Success, string? ErrorMessage = null);