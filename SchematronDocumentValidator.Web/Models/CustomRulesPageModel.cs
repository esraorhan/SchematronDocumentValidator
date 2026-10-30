using SchematronDocumentValidator.Core.Enums;
using SchematronDocumentValidator.Core.Models;

namespace SchematronDocumentValidator.Web.Models;

public sealed class CustomRulesPageModel
{
    public CustomRuleInputModel Input { get; set; } = new();

    public IReadOnlyList<CustomSchematronRule> Rules { get; set; }
        = Array.Empty<CustomSchematronRule>();
}

public sealed class CustomRuleInputModel
{
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public SchematronDocumentType[] DocumentTypes { get; set; }
        = Array.Empty<SchematronDocumentType>();

    // Birden fazla profil virgülle ayrılabilir.
    public string? ProfileIds { get; set; }

    public string Context { get; set; } = "";

    public string Test { get; set; } = "";

    public string Message { get; set; } = "";

    // Her satır: önek=namespace adresi
    public string NamespacesText { get; set; } =
        "cbc=urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2\n" +
        "cac=urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
}
