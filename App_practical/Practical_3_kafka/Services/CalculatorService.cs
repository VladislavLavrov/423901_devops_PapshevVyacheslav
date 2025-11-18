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

    public async Task<bool> DeleteCalculationAsync(int id)
    {
        var history = await _context.CalculationHistories.FindAsync(id);
        if (history != null)
        {
            _context.CalculationHistories.Remove(history);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public static async Task<CalculationResult> UpdateCalculationAsync(int id, double operand1, double operand2, string operation)
    {
        try
        {
            var history = await _context.CalculationHistories.FindAsync(id);
            if (history == null)
            {
                return new CalculationResult(null, false, "Запись не найдена");
            }

            double? result = operation.ToLower() switch
            {
                "сложение" => Models.Calculator.Sum(operand1, operand2),
                "вычитание" => Models.Calculator.Minus(operand1, operand2),
                "деление" => Models.Calculator.Division(operand1, operand2),
                "умножение" => Models.Calculator.Multiply(operand1, operand2),
                _ => throw new ArgumentException($"Недопустимая операция: {operation}")
            };

            history.Operand1 = operand1;
            history.Operand2 = operand2;
            history.Operation = operation;
            history.Result = result;
            history.CreatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return new CalculationResult(result, true);
        }
        catch (Exception ex)
        {
            return new CalculationResult(null, false, ex.Message);
        }
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