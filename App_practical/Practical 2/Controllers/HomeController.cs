using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calculator.Models;
using Calculator.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Calculator.Data;


namespace Calculator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICalculatorService _calculatorService;

    public HomeController(ILogger<HomeController> logger, ICalculatorService calculatorService)
    {
        _logger = logger;
        _calculatorService = calculatorService;
    }

    public IActionResult Index()
    {
        return View((object?)null);
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
