using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calculator.Models;
using Calculator.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Calculator.Data;
using System.Threading.Tasks;
using Prometheus;

namespace Calculator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICalculatorService _calculatorService;
    
    // Метрики Prometheus
    private static readonly Counter CalculationRequests = Metrics
        .CreateCounter("calculator_requests_total", "Total calculator requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "status" }
            });
    
    private static readonly Histogram CalculationDuration = Metrics
        .CreateHistogram("calculator_request_duration_seconds",
            "Calculator request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation" },
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10) // 0.01, 0.02, 0.04, ...
            });
    
    private static readonly Gauge ActiveCalculations = Metrics
        .CreateGauge("calculator_active_calculations", "Number of active calculations");
    
    private static readonly Counter DatabaseOperations = Metrics
        .CreateCounter("calculator_database_operations_total", "Total database operations",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "status" }
            });
    
    private static readonly Histogram DatabaseOperationDuration = Metrics
        .CreateHistogram("calculator_database_operation_duration_seconds",
            "Database operation duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation_type" },
                Buckets = Histogram.LinearBuckets(0.01, 0.05, 10)
            });

    public HomeController(ILogger<HomeController> logger, ICalculatorService calculatorService)
    {
        _logger = logger;
        _calculatorService = calculatorService;
    }

    public IActionResult Index()
    {
        // Инкремент счетчика просмотров главной страницы
        var viewCounter = Metrics.CreateCounter("calculator_page_views_total", 
            "Total page views", new CounterConfiguration
            {
                LabelNames = new[] { "page" }
            });
        viewCounter.WithLabels("index").Inc();
        
        return View((object?)null);
    }

    public async Task<IActionResult> Database()
    {
        try
        {
            // Измерение времени выполнения операции с БД
            using (DatabaseOperationDuration.WithLabels("get_history").NewTimer())
            {
                var result = await _calculatorService.GetHistoryAsync();
                
                // Успешная операция с БД
                DatabaseOperations.WithLabels("get_history", "success").Inc();
                
                var viewCounter = Metrics.CreateCounter("calculator_page_views_total", 
                    "Total page views", new CounterConfiguration
                    {
                        LabelNames = new[] { "page" }
                    });
                viewCounter.WithLabels("database").Inc();
                
                return View(result);
            }
        }
        catch (Exception ex)
        {
            // Ошибка операции с БД
            DatabaseOperations.WithLabels("get_history", "error").Inc();
            _logger.LogError(ex, "Ошибка при получении истории из БД");
            TempData["ErrorMessage"] = "Ошибка при загрузке истории вычислений";
            return View(new List<CalculationResult>());
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            using (DatabaseOperationDuration.WithLabels("get_by_id").NewTimer())
            {
                var calculation = await _calculatorService.GetCalculationByIdAsync(id);
                
                if (calculation == null)
                {
                    DatabaseOperations.WithLabels("get_by_id", "not_found").Inc();
                    TempData["ErrorMessage"] = "Запись не найдена!";
                    return RedirectToAction("Database");
                }
                
                DatabaseOperations.WithLabels("get_by_id", "success").Inc();
                return View(calculation);
            }
        }
        catch (Exception ex)
        {
            DatabaseOperations.WithLabels("get_by_id", "error").Inc();
            _logger.LogError(ex, "Ошибка при загрузке формы редактирования");
            TempData["ErrorMessage"] = "Ошибка при загрузке данных для редактирования";
            return RedirectToAction("Database");
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            using (DatabaseOperationDuration.WithLabels("get_by_id").NewTimer())
            {
                var calculation = await _calculatorService.GetCalculationByIdAsync(id);
                
                if (calculation == null)
                {
                    DatabaseOperations.WithLabels("get_by_id", "not_found").Inc();
                    TempData["ErrorMessage"] = "Запись не найдена!";
                    return RedirectToAction("Database");
                }
                
                DatabaseOperations.WithLabels("get_by_id", "success").Inc();
                return View(calculation);
            }
        }
        catch (Exception ex)
        {
            DatabaseOperations.WithLabels("get_by_id", "error").Inc();
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
            using (DatabaseOperationDuration.WithLabels("delete").NewTimer())
            {
                var result = await _calculatorService.DeleteCalculationAsync(id);
                
                if (result)
                {
                    DatabaseOperations.WithLabels("delete", "success").Inc();
                    TempData["SuccessMessage"] = "Запись успешно удалена!";
                }
                else
                {
                    DatabaseOperations.WithLabels("delete", "not_found").Inc();
                    TempData["ErrorMessage"] = "Запись не найдена!";
                }
            }
        }
        catch (Exception ex)
        {
            DatabaseOperations.WithLabels("delete", "error").Inc();
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
            using (DatabaseOperationDuration.WithLabels("update").NewTimer())
            {
                var result = await _calculatorService.UpdateCalculationAsync(id, operand1, operand2, operation);
                
                if (result.Success)
                {
                    DatabaseOperations.WithLabels("update", "success").Inc();
                    TempData["SuccessMessage"] = "Запись успешно обновлена!";
                }
                else
                {
                    DatabaseOperations.WithLabels("update", "error").Inc();
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }
            }
        }
        catch (Exception ex)
        {
            DatabaseOperations.WithLabels("update", "error").Inc();
            _logger.LogError(ex, "Ошибка при редактировании записи");
            TempData["ErrorMessage"] = "Произошла ошибка при обновлении записи";
        }
        
        return RedirectToAction("Database");
    }

    [HttpPost]
    public async Task<IActionResult> Index(string value1, string value2, string operation)
    {
        string? res = null;
        
        // Увеличиваем счетчик активных вычислений
        ActiveCalculations.Inc();
        
        try
        {
            // Измеряем время выполнения вычисления
            using (CalculationDuration.WithLabels(operation).NewTimer())
            {
                Console.WriteLine($"Я здесь: {value1} {value2} {operation}");

                if (!double.TryParse(value1.Replace('.', ','), out double v1))
                {
                    res = "Неправильный ввод числа 1.";
                    CalculationRequests.WithLabels(operation, "input_error").Inc();
                }
                else if (!double.TryParse(value2.Replace('.', ','), out double v2))
                {
                    res = "Неправильный ввод числа 2.";
                    CalculationRequests.WithLabels(operation, "input_error").Inc();
                }
                else
                {
                    try
                    {
                        var calculationResult = await _calculatorService.CalculateAsync(v1, v2, operation);
                        
                        if (calculationResult.Success)
                        {
                            res = $"Результат: {calculationResult.Result}";
                            CalculationRequests.WithLabels(operation, "success").Inc();
                            
                            // Метрика для успешных операций по типу
                            var successCounter = Metrics.CreateCounter(
                                "calculator_successful_operations_total",
                                "Total successful operations",
                                new CounterConfiguration
                                {
                                    LabelNames = new[] { "operation" }
                                });
                            successCounter.WithLabels(operation).Inc();
                        }
                        else
                        {
                            res = $"Ошибка: {calculationResult.ErrorMessage}";
                            CalculationRequests.WithLabels(operation, "calculation_error").Inc();
                            
                            // Метрика для ошибок по типу
                            var errorCounter = Metrics.CreateCounter(
                                "calculator_error_operations_total",
                                "Total error operations",
                                new CounterConfiguration
                                {
                                    LabelNames = new[] { "operation", "error_type" }
                                });
                            errorCounter.WithLabels(operation, "calculation").Inc();
                        }
                    }
                    catch (Exception ex)
                    {
                        res = $"Ошибка при расчете: {ex.Message}";
                        _logger.LogError(ex, "Ошибка в калькуляторе");
                        CalculationRequests.WithLabels(operation, "exception").Inc();
                        
                        var errorCounter = Metrics.CreateCounter(
                            "calculator_error_operations_total",
                            "Total error operations",
                            new CounterConfiguration
                            {
                                LabelNames = new[] { "operation", "error_type" }
                            });
                        errorCounter.WithLabels(operation, "exception").Inc();
                    }
                }
            }
        }
        finally
        {
            // Уменьшаем счетчик активных вычислений
            ActiveCalculations.Dec();
        }

        return View((object?)res);
    }

    public IActionResult Privacy()
    {
        var viewCounter = Metrics.CreateCounter("calculator_page_views_total", 
            "Total page views", new CounterConfiguration
            {
                LabelNames = new[] { "page" }
            });
        viewCounter.WithLabels("privacy").Inc();
        
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Метрика для ошибок
        var errorCounter = Metrics.CreateCounter("calculator_http_errors_total",
            "Total HTTP errors",
            new CounterConfiguration
            {
                LabelNames = new[] { "status_code" }
            });
        
        var statusCode = Response.StatusCode.ToString();
        errorCounter.WithLabels(statusCode).Inc();
        
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    // Новый метод для мониторинга здоровья
    public IActionResult Health()
    {
        // Метрика для проверок здоровья
        var healthCheckCounter = Metrics.CreateCounter("calculator_health_checks_total",
            "Total health checks");
        healthCheckCounter.Inc();
        
        return Ok("Healthy");
    }
}