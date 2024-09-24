using Autodesk.Revit.UI;

internal class CriarFiltroHandler : IExternalEventHandler
{
    public string SelectedCategory { get; set; }
    public string SelectedParameter { get; set; }
    public List<string> Values { get; set; }
    public string Prefix { get; set; }

    public void Execute(UIApplication app)
    {
        Document doc = app.ActiveUIDocument.Document;
        View activeView = doc.ActiveView; 

        using (Transaction trans = new Transaction(doc, "Aplicar Filtros"))
        {
            try
            {
                trans.Start();

                var category = doc.Settings.Categories.get_Item(SelectedCategory);
                if (category == null)
                {
                    TaskDialog.Show("Erro", $"Categoria '{SelectedCategory}' não encontrada.");
                    trans.RollBack();
                    return;
                }

                List<ElementId> categoryIds = new List<ElementId> { category.Id };

                foreach (var value in Values)
                {
                    ElementParameterFilter parameterFilter = CreateParameterFilter(doc, SelectedParameter, new List<string> { value });

                    if (parameterFilter != null)
                    {
                        string filterName = GenerateDynamicFilterName(doc, Prefix);
                        ParameterFilterElement filter = ParameterFilterElement.Create(doc, filterName, categoryIds, parameterFilter);

                        if (filter == null)
                        {
                            TaskDialog.Show("Erro", "Falha ao criar o filtro.");
                        }
                        else
                        {
                            OverrideGraphicSettings settings = new OverrideGraphicSettings();
                            settings.SetProjectionLineColor(new Autodesk.Revit.DB.Color(255, 0, 0)); 

                            activeView.AddFilter(filter.Id); 
                            activeView.SetFilterOverrides(filter.Id, settings); 
                        }
                    }
                    else
                    {
                        TaskDialog.Show("Erro", "Filtro de parâmetros não criado.");
                    }
                }

                trans.Commit();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erro", $"Erro ao criar filtro: {ex.Message}");
                trans.RollBack();
            }
        }
    }
    private string GenerateDynamicFilterName(Document doc, string userProvidedPrefix)
    {
        string baseName = string.IsNullOrEmpty(userProvidedPrefix) ? "Filtro" : userProvidedPrefix;

        int count = 1;
        string uniqueName = $"{baseName}{count}";

        while (new FilteredElementCollector(doc)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>()
            .Any(f => f.Name == uniqueName))
        {
            uniqueName = $"{baseName}{++count}";
        }

        return uniqueName;
    }

    public string GetName()
    {
        return "CriarFiltroHandler";
    }

    private ElementParameterFilter CreateParameterFilter(Document doc, string parameterName, List<string> parameterValues)
    {
        var parameter = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .FirstOrDefault(e => e.LookupParameter(parameterName) != null)
            ?.LookupParameter(parameterName);

        if (parameter == null)
        {
            TaskDialog.Show("Erro", $"Parâmetro '{parameterName}' não encontrado.");
            return null;
        }

        List<FilterRule> rules = new List<FilterRule>();

        foreach (var parameterValue in parameterValues)
        {
            FilterRule rule = null;

            if (parameter.StorageType == StorageType.String)
            {
                rule = ParameterFilterRuleFactory.CreateEqualsRule(parameter.Id, parameterValue);
            }
            else if (parameter.StorageType == StorageType.Double)
            {
                if (double.TryParse(parameterValue.Replace(" m", ""), out double valueAsDouble))
                {
                    ForgeTypeId specTypeId = parameter.Definition.GetDataType();
                    ForgeTypeId projectUnitType = GetProjectUnitType(doc, specTypeId);
                    double valueInInternalUnits = UnitUtils.ConvertToInternalUnits(valueAsDouble, projectUnitType);
                    rule = ParameterFilterRuleFactory.CreateEqualsRule(parameter.Id, valueInInternalUnits, 0.01);
                }
                else
                {
                    TaskDialog.Show("Erro", "Valor de parâmetro para tipo Double inválido.");
                    return null;
                }
            }
            else if (parameter.StorageType == StorageType.Integer)
            {
                if (int.TryParse(parameterValue, out int valueAsInt))
                {
                    rule = ParameterFilterRuleFactory.CreateEqualsRule(parameter.Id, valueAsInt);
                }
                else
                {
                    TaskDialog.Show("Erro", "Valor de parâmetro para tipo Integer inválido.");
                    return null;
                }
            }

            if (rule != null)
            {
                rules.Add(rule);
            }
        }

        if (rules.Count == 0)
        {
            TaskDialog.Show("Erro", "Nenhuma regra de filtro criada.");
            return null;
        }

        return new ElementParameterFilter(rules);
    }

    private ForgeTypeId GetProjectUnitType(Document doc, ForgeTypeId specTypeId)
    {
        FormatOptions formatOptions = doc.GetUnits().GetFormatOptions(specTypeId);
        return formatOptions.GetUnitTypeId();
    }
}