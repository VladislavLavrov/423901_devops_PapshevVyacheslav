using Calculator.Data;
using Calculator.Models;
using Microsoft.EntityFrameworkCore;

namespace Calculator.Services;

public class CalculatorService : ICalculatorService
{
    private readonly CalculatorContext _context;

    public CalculatorService(CalculatorContext context)
    {
        _context = context;
    }

    public async Task<CalculationResult> CalculateAsync(double a, double b, string operation)
    {
        try
        {
            double? result = operation.ToLower() switch
            {
                "сложение" or "add" or "sum" => Models.Calculator.Sum(a, b),
                "вычитание" or "subtract" or "minus" => Models.Calculator.Minus(a, b),
                "деление" or "divide" or "division" => Models.Calculator.Division(a, b),
                "умножение" or "multiply" => Models.Calculator.Multiply(a, b),
                _ => throw new ArgumentException($"Invalid operation: {operation}")
            };

            var history = new CalculationHistory
            {
                Operand1 = a,
                Operand2 = b,
                Operation = operation,
                Result = result
            };

            _context.CalculationHistories.Add(history);
            await _context.SaveChangesAsync();

            return new CalculationResult(result, true);
        }
        catch (Exception ex)
        {
            var errorHistory = new CalculationHistory
            {
                Operand1 = a,
                Operand2 = b,
                Operation = operation,
                Result = null
            };

            _context.CalculationHistories.Add(errorHistory);
            await _context.SaveChangesAsync();

            return new CalculationResult(null, false, ex.Message);
        }
    }

    public async Task<List<CalculationHistory>> GetHistoryAsync()
    {
        return await _context.CalculationHistories
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<CalculationHistory?> GetCalculationByIdAsync(int id)
    {
        return await _context.CalculationHistories
            .FirstOrDefaultAsync(h => h.Id == id);
    }
}