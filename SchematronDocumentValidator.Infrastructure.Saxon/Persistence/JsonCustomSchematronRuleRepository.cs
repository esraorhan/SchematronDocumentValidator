using SchematronDocumentValidator.Core.Abstractions;
using SchematronDocumentValidator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Persistence
{

    public sealed class JsonCustomSchematronRuleRepository : ICustomSchematronRuleRepository
    {
        private readonly string _filePath;

        // Aynı uygulamada eş zamanlı kayıtların birbirini ezmesini engeller.
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters =
        {
            new JsonStringEnumConverter()
        }
        };

        public JsonCustomSchematronRuleRepository(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            _filePath = Path.GetFullPath(filePath);
        }

        public async Task<IReadOnlyList<CustomSchematronRule>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);

            try
            {
                return await ReadAsync(cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SaveAsync(CustomSchematronRule rule, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (rule.Id == Guid.Empty)
            {
                throw new ArgumentException("Kural kimliği boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(rule.Code))
            {
                throw new ArgumentException("Kural kodu boş olamaz.");
            }

            if (rule.Scope is null ||
                rule.Scope.DocumentTypes is null ||
                rule.Scope.DocumentTypes.Count == 0 ||
                rule.Scope.ProfileIds is null ||
                rule.Namespaces is null ||
                string.IsNullOrWhiteSpace(rule.Context) ||
                string.IsNullOrWhiteSpace(rule.Test))
            {
                throw new ArgumentException(
                    "Kural kapsamı, namespace listesi, Context ve Test geçerli olmalıdır.");
            }

            await _lock.WaitAsync(cancellationToken);

            try
            {
                var rules = await ReadAsync(cancellationToken);

                // Güncellenen kaydı kendi kendisiyle karşılaştırma.
                var otherRules = rules
                    .Where(existing => existing.Id != rule.Id)
                    .ToList();

                var sameCodeRule = otherRules.FirstOrDefault(existing =>
                    string.Equals(
                        existing.Code?.Trim(),
                        rule.Code.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (sameCodeRule is not null)
                {
                    throw new InvalidOperationException(
                        $"'{rule.Code}' koduyla başka bir kural zaten kayıtlı.");
                }

                var sameDefinitionRule = otherRules.FirstOrDefault(existing =>
                    HasSameDefinition(existing, rule));

                if (sameDefinitionRule is not null)
                {
                    throw new InvalidOperationException(
                        $"Aynı kapsam ve kontrol koşulu " +
                        $"'{sameDefinitionRule.Code}' kodlu " +
                        $"'{sameDefinitionRule.Name}' kuralında zaten tanımlı. " +
                        "Yeni kayıt yerine mevcut kuralı kullanın.");
                }

                int index = rules.FindIndex(existing => existing.Id == rule.Id);

                if (index >= 0)
                {
                    rules[index] = rule;
                }
                else
                {
                    rules.Add(rule);
                }

                await WriteAsync(rules, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        private static bool HasSameDefinition(
    CustomSchematronRule first,
    CustomSchematronRule second)
        {
            // Belge türlerinin sırası ve tekrarları karşılaştırmayı etkilemez.
            bool sameDocumentTypes = first.Scope.DocumentTypes
                .Distinct()
                .OrderBy(type => type)
                .SequenceEqual(
                    second.Scope.DocumentTypes
                        .Distinct()
                        .OrderBy(type => type));

            if (!sameDocumentTypes)
            {
                return false;
            }

            // Profil değerlerinde büyük/küçük harf korunur.
            // Boş liste, bütün profilleri ifade eder.
            bool sameProfiles = NormalizeProfiles(first.Scope.ProfileIds)
                .SequenceEqual(
                    NormalizeProfiles(second.Scope.ProfileIds),
                    StringComparer.Ordinal);

            if (!sameProfiles)
            {
                return false;
            }

            // Yalnızca baştaki ve sondaki boşluklar temizlenir.
            // XPath içindeki metinlere ve boşluklara dokunulmaz.
            bool sameContext = string.Equals(
                first.Context.Trim(),
                second.Context.Trim(),
                StringComparison.Ordinal);

            bool sameTest = string.Equals(
                first.Test.Trim(),
                second.Test.Trim(),
                StringComparison.Ordinal);

            if (!sameContext || !sameTest)
            {
                return false;
            }

            // Namespace tanımlarının eklenme sırası önemli değildir.
            // Önekler ve adresler birebir karşılaştırılır.
            return first.Namespaces
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .SequenceEqual(
                    second.Namespaces
                        .OrderBy(item => item.Key, StringComparer.Ordinal));
        }

        private static IEnumerable<string> NormalizeProfiles(
            IReadOnlyList<string> profileIds)
        {
            return profileIds
                .Select(profile => profile.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(profile => profile, StringComparer.Ordinal);
        }
        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);

            try
            {
                var rules = await ReadAsync(cancellationToken);

                int removedCount = rules.RemoveAll(rule => rule.Id == id);

                if (removedCount > 0)
                {
                    await WriteAsync(rules, cancellationToken);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<List<CustomSchematronRule>> ReadAsync(
            CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
            {
                return new List<CustomSchematronRule>();
            }

            await using var stream = File.OpenRead(_filePath);

            var rules =
                await JsonSerializer.DeserializeAsync<List<CustomSchematronRule>>(
                    stream,
                    JsonOptions,
                    cancellationToken);

            if (rules is null || rules.Any(rule => rule is null))
            {
                throw new InvalidDataException(
                    "Özel kural dosyasının içeriği geçersiz.");
            }

            return rules;
        }

        private async Task WriteAsync(
            List<CustomSchematronRule> rules,
            CancellationToken cancellationToken)
        {
            string directory = Path.GetDirectoryName(_filePath)!;

            Directory.CreateDirectory(directory);

            string temporaryPath = Path.Combine(
                directory,
                $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                await using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        rules,
                        JsonOptions,
                        cancellationToken);

                    await stream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Yeni içerik yazıldıktan sonra asıl dosyayı değiştirir.
                File.Move(temporaryPath, _filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
