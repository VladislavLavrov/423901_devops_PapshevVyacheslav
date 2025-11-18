using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calculator.Models;
using Calculator.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Calculator.Data;
using System.Threading.Tasks;
using Confluent.Kafka;


namespace Calculator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICalculatorService _calculatorService;
    private readonly KafkaProducerService<Null,string> _producer;

    public HomeController(ILogger<HomeController> logger, ICalculatorService calculatorService, KafkaProducerService<Null, string> producer)
    {
        _logger = logger;
        _calculatorService = calculatorService;
        _producer = producer;
    }

    public IActionResult Index()
    {
        return View((object?)null);
    }

    public async Task<IActionResult> Database()
    {
        var result = await _calculatorService.GetHistoryAsync();
        return View(result);
    }


    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var calculation = await _calculatorService.GetCalculationByIdAsync(id);
            if (calculation == null)
            {
                TempData["ErrorMessage"] = "Запись не найдена!";
                return RedirectToAction("Database");
            }

            return View(calculation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке формы редактирования");
            TempData["ErrorMessage"] = "Ошибка при загрузке данных для редактирования";
            return RedirectToAction("Database");
        }
    }

    private async Task SendDataToKafka(CalculationHistory dataInputVariant)
    {
        var json = JsonSerializer.Serialize(dataInputVariant);
        await _producer.ProduceAsync("10_calculator", new Message<Null, string> { Value = json });
    }


    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var calculation = await _calculatorService.GetCalculationByIdAsync(id);
            if (calculation == null)
            {
                TempData["ErrorMessage"] = "Запись не найдена!";
                return RedirectToAction("Database");
            }

            return View(calculation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке формы удаления");
            TempData["ErrorMessage"] = "Ошибка при загрузке данных для удаления";
            return RedirectToAction("Database");
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteConfirm(int id)
    {
        try
        {
            var result = await _calculatorService.DeleteCalculationAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Запись успешно удалена!";
            }
            else
            {
                TempData["ErrorMessage"] = "Запись не найдена!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении записи");
            TempData["ErrorMessage"] = "Произошла ошибка при удалении записи";
        }

        return RedirectToAction("Database");
    }

    // Метод для редактирования записи
    [HttpPost]
    public async Task<IActionResult> Edit(int id, double operand1, double operand2, string operation)
    {
        try
        {
            var result = await _calculatorService.UpdateCalculationAsync(id, operand1, operand2, operation);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Запись успешно обновлена!";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при редактировании записи");
            TempData["ErrorMessage"] = "Произошла ошибка при обновлении записи";
        }

        return RedirectToAction("Database");
    }

    [HttpPost]
    public async Task<IActionResult> Index(string value1, string value2, string operation)
    {
        string? res = null;
        Console.WriteLine($"Я здесь: {value1} {value2} {operation}");

        if (!double.TryParse(value1.Replace('.', ','), out double v1))
        {
            res = "Неправильный ввод числа 1.";
        }
        else if (!double.TryParse(value2.Replace('.', ','), out double v2))
        {
            res = "Неправильный ввод числа 2.";
        }
        else
        {
            try
            {
                var calculationResult = await _calculatorService.CalculateAsync(v1, v2, operation);
                if (calculationResult.Success)
                {
                    res = $"Результат: {calculationResult.Result}";
                }
                else
                {
                    res = $"Ошибка: {calculationResult.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                res = $"Ошибка при расчете: {ex.Message}";
                _logger.LogError(ex, "Ошибка в калькуляторе");
            }
        }

        return View((object?)res);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
