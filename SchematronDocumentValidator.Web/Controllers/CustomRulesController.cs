using Microsoft.AspNetCore.Mvc;
using SchematronDocumentValidator.Application.Validation;
using SchematronDocumentValidator.Core.Abstractions;
using SchematronDocumentValidator.Core.Models;
using SchematronDocumentValidator.Web.Models;

namespace SchematronDocumentValidator.Web.Controllers;

public sealed class CustomRulesController : Controller
{
    private readonly ICustomSchematronRuleRepository _repository;
    private readonly CustomSchematronRuleValidator _validator;
    private readonly ILogger<CustomRulesController> _logger;

    public CustomRulesController(
        ICustomSchematronRuleRepository repository,
        CustomSchematronRuleValidator validator,
        ILogger<CustomRulesController> logger)
    {
        _repository = repository;
        _validator = validator;
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
}