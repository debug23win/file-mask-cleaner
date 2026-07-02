namespace FileMaskCleaner.Gui;

public class MainForm : Form
{
    private readonly TextBox _folderBox;
    private readonly TextBox _maskBox;
    private readonly TextBox _targetBox;
    private readonly CheckBox _recursiveCheck;
    private readonly CheckBox _moveCheck;
    private readonly Button _browseTargetButton;
    private readonly Button _findButton;
    private readonly Button _actButton;
    private readonly CheckedListBox _filesList;
    private readonly Label _statusLabel;

    public MainForm()
    {
        Text = "FileMaskCleaner — удаление файлов по маске";
        MinimumSize = new Size(560, 480);
        Size = new Size(720, 560);
        StartPosition = FormStartPosition.CenterScreen;

        var folderLabel = new Label { Text = "Папка:", Location = new Point(12, 15), AutoSize = true };
        _folderBox = new TextBox
        {
            Location = new Point(110, 12),
            Width = ClientSize.Width - 110 - 100,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        var browseFolderButton = new Button
        {
            Text = "Обзор…",
            Location = new Point(ClientSize.Width - 92, 11),
            Width = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        browseFolderButton.Click += (_, _) => BrowseInto(_folderBox, "Выберите папку для поиска файлов");

        var maskLabel = new Label { Text = "Маска:", Location = new Point(12, 47), AutoSize = true };
        _maskBox = new TextBox { Text = "копия", Location = new Point(110, 44), Width = 200 };
        var maskHint = new Label
        {
            Text = "* и ? поддерживаются; без них — поиск подстроки",
            Location = new Point(320, 47),
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
        };

        _recursiveCheck = new CheckBox { Text = "Включая подпапки", Location = new Point(110, 74), AutoSize = true };

        _moveCheck = new CheckBox
        {
            Text = "Не удалять, а перемещать найденные файлы в папку:",
            Location = new Point(110, 100),
            AutoSize = true,
        };
        _targetBox = new TextBox
        {
            Location = new Point(110, 126),
            Width = ClientSize.Width - 110 - 100,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Enabled = false,
        };
        _browseTargetButton = new Button
        {
            Text = "Обзор…",
            Location = new Point(ClientSize.Width - 92, 125),
            Width = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false,
        };
        _browseTargetButton.Click += (_, _) => BrowseInto(_targetBox, "Выберите папку, куда переместить файлы");
        _moveCheck.CheckedChanged += (_, _) =>
        {
            _targetBox.Enabled = _moveCheck.Checked;
            _browseTargetButton.Enabled = _moveCheck.Checked;
            UpdateActButton();
        };

        _findButton = new Button { Text = "Найти", Location = new Point(110, 158), Width = 120, Height = 30 };
        _findButton.Click += (_, _) => FindFiles();

        _actButton = new Button
        {
            Text = "Удалить отмеченные",
            Location = new Point(240, 158),
            Width = 180,
            Height = 30,
            Enabled = false,
        };
        _actButton.Click += (_, _) => ProcessFiles();

        _filesList = new CheckedListBox
        {
            Location = new Point(12, 198),
            Size = new Size(ClientSize.Width - 24, ClientSize.Height - 198 - 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            CheckOnClick = true,
            HorizontalScrollbar = true,
        };

        _statusLabel = new Label
        {
            Text = "Укажите папку и маску, затем нажмите «Найти».",
            Location = new Point(12, ClientSize.Height - 26),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        };

        Controls.AddRange(
        [
            folderLabel, _folderBox, browseFolderButton,
            maskLabel, _maskBox, maskHint,
            _recursiveCheck,
            _moveCheck, _targetBox, _browseTargetButton,
            _findButton, _actButton,
            _filesList, _statusLabel,
        ]);
    }

    private static void BrowseInto(TextBox box, string description)
    {
        using var dialog = new FolderBrowserDialog { Description = description, UseDescriptionForTitle = true };
        if (Directory.Exists(box.Text))
            dialog.InitialDirectory = box.Text;
        if (dialog.ShowDialog() == DialogResult.OK)
            box.Text = dialog.SelectedPath;
    }

    private void UpdateActButton()
    {
        _actButton.Text = _moveCheck.Checked ? "Переместить отмеченные" : "Удалить отмеченные";
    }

    private void FindFiles()
    {
        string folder = _folderBox.Text.Trim();
        string mask = _maskBox.Text.Trim();

        if (!Directory.Exists(folder))
        {
            MessageBox.Show(this, "Папка не найдена: " + folder, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (mask.Length == 0)
        {
            MessageBox.Show(this, "Укажите маску имени файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Без подстановочных символов маска трактуется как подстрока: "копия" -> "*копия*"
        string pattern = mask.Contains('*') || mask.Contains('?') ? mask : $"*{mask}*";

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = _recursiveCheck.Checked,
            MatchCasing = MatchCasing.CaseInsensitive,
            IgnoreInaccessible = true,
        };

        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(folder, pattern, options).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Ошибка поиска: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _filesList.Items.Clear();
        foreach (var file in files)
            _filesList.Items.Add(file, isChecked: true);

        _actButton.Enabled = files.Count > 0;
        _statusLabel.Text = files.Count == 0
            ? $"Файлы по маске «{pattern}» не найдены."
            : $"Найдено файлов: {files.Count}. Снимите галочки с тех, которые нужно оставить.";
    }

    private void ProcessFiles()
    {
        var files = _filesList.CheckedItems.Cast<string>().ToList();
        if (files.Count == 0)
        {
            MessageBox.Show(this, "Не отмечен ни один файл.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        bool move = _moveCheck.Checked;
        string target = _targetBox.Text.Trim();

        if (move && target.Length == 0)
        {
            MessageBox.Show(this, "Укажите папку, куда перемещать файлы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string question = move
            ? $"Переместить {files.Count} файл(ов) в папку:\n{target}?"
            : $"Безвозвратно удалить {files.Count} файл(ов)?";
        if (MessageBox.Show(this, question, "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        if (move)
        {
            try
            {
                Directory.CreateDirectory(target);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось создать папку назначения: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        int done = 0;
        var errors = new List<string>();
        foreach (var file in files)
        {
            try
            {
                if (move)
                    File.Move(file, GetUniquePath(target, Path.GetFileName(file)));
                else
                    File.Delete(file);
                done++;
                _filesList.Items.Remove(file);
            }
            catch (Exception ex)
            {
                errors.Add($"{file}: {ex.Message}");
            }
        }

        _actButton.Enabled = _filesList.Items.Count > 0;
        string verb = move ? "Перемещено" : "Удалено";
        _statusLabel.Text = $"{verb}: {done}, ошибок: {errors.Count}.";
        if (errors.Count > 0)
            MessageBox.Show(this, string.Join("\n", errors.Take(20)), "Не все файлы обработаны",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // При совпадении имён в папке назначения добавляет суффикс: file.txt -> file (1).txt
    private static string GetUniquePath(string folder, string fileName)
    {
        string path = Path.Combine(folder, fileName);
        if (!File.Exists(path))
            return path;

        string name = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);
        for (int i = 1; ; i++)
        {
            path = Path.Combine(folder, $"{name} ({i}){ext}");
            if (!File.Exists(path))
                return path;
        }
    }
}
