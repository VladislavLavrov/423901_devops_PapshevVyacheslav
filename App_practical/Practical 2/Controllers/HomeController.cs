using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calculator.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;


namespace Calculator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View((object?)null);
    }

    [HttpPost]
    public IActionResult Index(string value1, string value2, string operation)
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
        else switch (operation)
        {
            case "Сложение": res = Convert.ToString(Models.Calculator.Sum(v1, v2)); break;
            case "Вычитание": res = Convert.ToString(Models.Calculator.Minus(v1, v2)); break;
            case "Умножение": res = Convert.ToString(Models.Calculator.Multiply(v1, v2)); break;
            case "Деление": res = Convert.ToString(Models.Calculator.Division(v1, v2)); break;
        }

        if (string.IsNullOrEmpty(res))
        {
            res = "Ошибка при расчете. Смотрите логи в консоли приложения";
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
