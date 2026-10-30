using Microsoft.AspNetCore.Mvc;
using SchematronDocumentValidator.Application.Validation;
using SchematronDocumentValidator.Core.Abstractions;
using SchematronDocumentValidator.Core.Models;
using SchematronDocumentValidator.Web.Models;
using SchematronDocumentValidator.Infrastructure.Saxon.Compilation;
namespace SchematronDocumentValidator.Web.Controllers;

public sealed class CustomRulesController : Controller
{
    private readonly ICustomSchematronRuleRepository _repository;
    private readonly CustomSchematronRuleValidator _validator;
    private readonly ILogger<CustomRulesController> _logger;
    private readonly CustomSchematronCompilationService _compilationService;
    private readonly IWebHostEnvironment _environment;
    public CustomRulesController(
        ICustomSchematronRuleRepository repository,
        CustomSchematronRuleValidator validator,
        CustomSchematronCompilationService compilationService,
        IWebHostEnvironment environment,
        ILogger<CustomRulesController> logger)
    {
        _repository = repository;
        _validator = validator;
        _compilationService = compilationService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index( CancellationToken cancellationToken)
    {
        return await ShowPageAsync(
            new CustomRuleInputModel(),
            cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Input")] CustomRuleInputModel input,
        CancellationToken cancellationToken)
    {
        var namespaces = ParseNamespaces(input.NamespacesText);

        var rule = new CustomSchematronRule
        {
            Code = input.Code?.Trim() ?? "",
            Name = input.Name?.Trim() ?? "",

            Scope = new CustomSchematronRuleScope
            {
                DocumentTypes = input.DocumentTypes
                    ?? Array.Empty<
                        SchematronDocumentValidator.Core.Enums.SchematronDocumentType>(),

                ProfileIds = (input.ProfileIds ?? "")
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            },

            Namespaces = namespaces,
            Context = input.Context?.Trim() ?? "",
            Test = input.Test?.Trim() ?? "",
            Message = input.Message?.Trim() ?? "",

            // Derleme kontrolü ve çalıştırma bağlantısı sonraki adımda.
            IsActive = false
        };

        foreach (string error in _validator.Validate(rule))
        {
            ModelState.AddModelError(string.Empty, error);
        }

        if (!ModelState.IsValid)
        {
            return await ShowPageAsync(input, cancellationToken);
        }

        try
        {
            await _repository.SaveAsync(rule, cancellationToken);

            TempData["Success"] =
                "Kural pasif olarak kaydedildi. Henüz belge doğrulamasında çalıştırılmıyor.";

            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(
                string.Empty,
                exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Özel kural kaydedilemedi.");

            ModelState.AddModelError(
                string.Empty,
                "Kural kaydedilemedi. Teknik ayrıntılar uygulama günlüğünde.");
        }

        return await ShowPageAsync(input, cancellationToken);
    }

    private Dictionary<string, string> ParseNamespaces(string? text)
    {
        var namespaces = new Dictionary<string, string>(
            StringComparer.Ordinal);

        var lines = (text ?? "").Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        foreach (string line in lines)
        {
            int separator = line.IndexOf('=');

            if (separator <= 0 || separator == line.Length - 1)
            {
                ModelState.AddModelError(
                    "Input.NamespacesText",
                    $"Namespace satırı 'önek=adres' biçiminde olmalıdır: {line}");

                continue;
            }

            string prefix = line[..separator].Trim();
            string address = line[(separator + 1)..].Trim();

            if (!namespaces.TryAdd(prefix, address))
            {
                ModelState.AddModelError(
                    "Input.NamespacesText",
                    $"'{prefix}' öneki birden fazla tanımlanmış.");
            }
        }

        return namespaces;
    }

    private async Task<IActionResult> ShowPageAsync(
        CustomRuleInputModel input,
        CancellationToken cancellationToken)
    {
        var model = new CustomRulesPageModel
        {
            Input = input
        };

        try
        {
            model.Rules = await _repository.GetAllAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Özel kurallar okunamadı.");

            ModelState.AddModelError(
                string.Empty,
                "Kural listesi okunamadı. Kayıt dosyasını kontrol edin.");
        }

        return View("Index", model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Compile(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _repository.GetAllAsync(cancellationToken);

            if (rules.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Derlenecek kural bulunamadı. Önce bir kural ekleyin.");

                return await ShowPageAsync(
                    new CustomRuleInputModel(),
                    cancellationToken);
            }

            // Her düğmeye basış ayrı bir derleme çalışmasıdır.
            string buildId = Guid.NewGuid().ToString("N");

            string buildDirectory = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "CustomSchematronBuilds",
                buildId);

            int successfulCount = 0;

            foreach (var rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // JSON'dan gelen kaydı da derlemeden önce kontrol et.
                var validationErrors = _validator.Validate(rule);

                if (validationErrors.Count > 0)
                {
                    foreach (string error in validationErrors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            $"{rule.Code}: {error}");
                    }

                    continue;
                }

                // Her kurala ayrı klasör: kaynak ve çıktılar karışmaz.
                string ruleDirectory = Path.Combine(
                    buildDirectory,
                    rule.Id.ToString("N"));

                try
                {
                    var artifacts = _compilationService.Compile(
                        new[] { rule },
                        ruleDirectory);

                    successfulCount++;

                    _logger.LogInformation(
                        "Özel kural derlendi. Kod: {RuleCode}, XSL: {XsltPath}",
                        rule.Code,
                        artifacts.GeneratedValidatorXsltPath);
                }
                catch (Saxon.Api.SaxonApiException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Kural derlenemedi: {RuleCode}",
                        rule.Code);

                    ModelState.AddModelError(
                        string.Empty,
                        $"{rule.Code}: Derleme başarısız. " +
                        $"Context, Test ve namespace tanımlarını kontrol edin. " +
                        $"Ayrıntı: {exception.Message}");
                }
                catch (InvalidOperationException exception)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{rule.Code}: {exception.Message}");
                }
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"{rules.Count} kuraldan {successfulCount} tanesi derlendi. " +
                    "Hatalı kurallar var; bu çalışma etkinleştirilmedi.");

                return await ShowPageAsync(
                    new CustomRuleInputModel(),
                    cancellationToken);
            }

            TempData["Success"] =
                $"{successfulCount} kural başarıyla derlendi. " +
                "Özel XSL dosyaları oluşturuldu. " +
                "Belge doğrulamasına henüz bağlanmadı.";

            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Özel kuralların derleme işlemi tamamlanamadı.");

            ModelState.AddModelError(
                string.Empty,
                "Derleme işlemi tamamlanamadı. " +
                "Teknik ayrıntılar uygulama günlüğünde.");

            return await ShowPageAsync(
                new CustomRuleInputModel(),
                cancellationToken);
        }
    }
}