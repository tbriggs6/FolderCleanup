using UtilitySuite.Core.Abstractions;
using UtilitySuite.Core.Runtime;
using UtilitySuite.Modules;

var registry = new UtilityRegistry();

foreach (var module in ModuleCatalog.GetDefaultModules())
{
    registry.Register(module);
}

var modules = registry.GetModules();
var run = true;

while (run)
{
    Console.Clear();
    Console.WriteLine("UtilitySuite (starter host)");
    Console.WriteLine("===========================");
    Console.WriteLine();

    for (var i = 0; i < modules.Count; i++)
    {
        Console.WriteLine($"{i + 1}. {modules[i].DisplayName}");
    }

    Console.WriteLine("0. Exit");
    Console.WriteLine();
    Console.Write("Select a utility: ");

    var selectedModule = ReadSelection(modules.Count);
    if (selectedModule == 0)
    {
        run = false;
        continue;
    }

    ShowModuleMenu(modules[selectedModule - 1]);
}

return;

static void ShowModuleMenu(IUtilityModule module)
{
    var run = true;

    while (run)
    {
        Console.Clear();
        Console.WriteLine(module.DisplayName);
        Console.WriteLine(new string('=', module.DisplayName.Length));
        Console.WriteLine(module.Description);
        Console.WriteLine();

        var actions = module.Actions;
        for (var i = 0; i < actions.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {actions[i].DisplayName}");
        }

        Console.WriteLine("0. Back");
        Console.WriteLine();
        Console.Write("Select an action: ");

        var selectedAction = ReadSelection(actions.Count);
        if (selectedAction == 0)
        {
            run = false;
            continue;
        }

        ExecuteAction(actions[selectedAction - 1]);
    }
}

static void ExecuteAction(IUtilityAction action)
{
    Console.Clear();
    Console.WriteLine(action.DisplayName);
    Console.WriteLine(new string('-', action.DisplayName.Length));
    Console.WriteLine(action.Description);
    Console.WriteLine();

    var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var entry in action.InputSchema)
    {
        Console.Write($"{entry.Key} ({entry.Value}): ");
        var value = Console.ReadLine() ?? string.Empty;
        inputs[entry.Key] = value;
    }

    var context = new UtilityActionContext(inputs);

    try
    {
        var result = action.ExecuteAsync(context).GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(result))
        {
            Console.WriteLine(result);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("Action failed:");
        Console.WriteLine(ex.Message);
    }

    Console.WriteLine();
    Console.WriteLine("Press Enter to continue...");
    Console.ReadLine();
}

static int ReadSelection(int max)
{
    while (true)
    {
        var value = Console.ReadLine();
        if (int.TryParse(value, out var parsed) && parsed >= 0 && parsed <= max)
        {
            return parsed;
        }

        Console.Write("Invalid selection. Try again: ");
    }
}
