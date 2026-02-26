using VegaFileConstructor.Models;

namespace VegaFileConstructor.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db, string webRoot)
    {
        if (db.DocumentTemplates.Any())
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(webRoot, "templates"));

        var fan = new DocumentTemplate
        {
            Name = "ИДП — Вентиляторный агрегат",
            Code = "IDP_FAN_V1",
            Description = "Шаблон ИДП для вентиляторных агрегатов",
            TemplateFilePath = "templates/idp_fan.pdf",
            Version = "1.0",
            IsActive = true
        };

        var pump = new DocumentTemplate
        {
            Name = "ИДП — Насосный агрегат",
            Code = "IDP_PUMP_V1",
            Description = "Шаблон ИДП для насосных агрегатов",
            TemplateFilePath = "templates/idp_pump.pdf",
            Version = "1.0",
            IsActive = true
        };

        var disabled = new DocumentTemplate
        {
            Name = "Архивный шаблон",
            Code = "ARCHIVE_V0",
            Description = "Отключенный шаблон для проверки фильтра",
            TemplateFilePath = "templates/archive.pdf",
            Version = "0.9",
            IsActive = false
        };

        db.DocumentTemplates.AddRange(fan, pump, disabled);

        var defs = new List<TemplateFieldDefinition>
        {
            new() { Template = fan, Key = "KksCode", Label = "KKS код", Group = "Шапка", Order = 1, IsRequired = true, MaxLength = 60, Placeholder = "ВА-4-III-7", HelpText = "Пример: ВА-4-III-7" },
            new() { Template = fan, Key = "DeviceCode", Label = "Код устройства", Group = "Шапка", Order = 2, IsRequired = true, MaxLength = 60 },
            new() { Template = fan, Key = "Executor", Label = "Исполнитель", Group = "Исполнитель", Order = 3, IsRequired = true, MaxLength = 120 },
            new() { Template = fan, Key = "IssueDate", Label = "Дата", Group = "Шапка", Order = 4, IsRequired = true, DataType = TemplateDataType.Date },
            new() { Template = pump, Key = "KksCode", Label = "KKS код", Group = "Шапка", Order = 1, IsRequired = true, MaxLength = 60 },
            new() { Template = pump, Key = "PumpModel", Label = "Модель насоса", Group = "Оборудование", Order = 2, IsRequired = true, MaxLength = 100 },
            new() { Template = pump, Key = "Customer", Label = "Заказчик", Group = "Шапка", Order = 3, IsRequired = false, MaxLength = 120 },
            new() { Template = pump, Key = "IssueDate", Label = "Дата", Group = "Шапка", Order = 4, IsRequired = true, DataType = TemplateDataType.Date }
        };
        db.TemplateFieldDefinitions.AddRange(defs);

        var places = new List<TemplateFieldPlacement>
        {
            new() { Template = fan, FieldKey = "KksCode", X = 80, Y = 760, FontSize = 11 },
            new() { Template = fan, FieldKey = "DeviceCode", X = 80, Y = 740, FontSize = 11 },
            new() { Template = fan, FieldKey = "Executor", X = 80, Y = 720, FontSize = 11 },
            new() { Template = fan, FieldKey = "IssueDate", X = 450, Y = 760, FontSize = 11 },
            new() { Template = pump, FieldKey = "KksCode", X = 80, Y = 760, FontSize = 11 },
            new() { Template = pump, FieldKey = "PumpModel", X = 80, Y = 740, FontSize = 11 },
            new() { Template = pump, FieldKey = "Customer", X = 80, Y = 720, FontSize = 11 },
            new() { Template = pump, FieldKey = "IssueDate", X = 450, Y = 760, FontSize = 11 }
        };
        db.TemplateFieldPlacements.AddRange(places);

        await db.SaveChangesAsync();
    }
}
