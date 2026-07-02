using System.Text;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length < 2 || args.Contains("-h") || args.Contains("--help"))
{
    PrintUsage();
    return args.Length < 2 ? 1 : 0;
}

string folder = args[0];
string mask = args[1];
bool recursive = args.Contains("-r") || args.Contains("--recursive");
bool dryRun = args.Contains("--dry-run");
bool yes = args.Contains("-y") || args.Contains("--yes");

if (!Directory.Exists(folder))
{
    Console.Error.WriteLine($"Ошибка: папка не найдена: {folder}");
    return 1;
}

// Если в маске нет подстановочных символов, ищем как подстроку: "копия" -> "*копия*"
string pattern = mask.Contains('*') || mask.Contains('?') ? mask : $"*{mask}*";

var options = new EnumerationOptions
{
    RecurseSubdirectories = recursive,
    MatchCasing = MatchCasing.CaseInsensitive,
    IgnoreInaccessible = true,
};

var files = Directory.EnumerateFiles(folder, pattern, options).ToList();

if (files.Count == 0)
{
    Console.WriteLine($"Файлы по маске \"{pattern}\" не найдены в: {folder}");
    return 0;
}

Console.WriteLine($"Найдено файлов по маске \"{pattern}\": {files.Count}");
foreach (var file in files)
    Console.WriteLine($"  {file}");

if (dryRun)
{
    Console.WriteLine("\nРежим --dry-run: ничего не удалено.");
    return 0;
}

if (!yes)
{
    Console.Write($"\nУдалить эти файлы ({files.Count} шт.)? [y/N]: ");
    string? answer = Console.ReadLine();
    if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Отменено.");
        return 0;
    }
}

int deleted = 0, failed = 0;
foreach (var file in files)
{
    try
    {
        File.Delete(file);
        deleted++;
        Console.WriteLine($"Удалён: {file}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"Не удалось удалить {file}: {ex.Message}");
    }
}

Console.WriteLine($"\nГотово. Удалено: {deleted}, ошибок: {failed}.");
return failed == 0 ? 0 : 2;

static void PrintUsage()
{
    Console.WriteLine("""
        FileMaskCleaner — удаляет из папки файлы, имена которых подходят под маску.

        Использование:
          FileMaskCleaner <папка> <маска> [опции]

        Аргументы:
          <папка>   Папка, в которой искать файлы.
          <маска>   Маска имени файла. Поддерживаются * и ?.
                    Если подстановочных символов нет, ищется подстрока
                    (например, "копия" означает "*копия*").

        Опции:
          -r, --recursive   Искать также во вложенных папках.
          --dry-run         Только показать, что будет удалено, без удаления.
          -y, --yes         Не спрашивать подтверждение.
          -h, --help        Показать эту справку.

        Примеры:
          FileMaskCleaner "C:\Docs" копия
          FileMaskCleaner "C:\Docs" "* - копия*" -r --dry-run
          FileMaskCleaner "C:\Docs" "*.tmp" -r -y
        """);
}
