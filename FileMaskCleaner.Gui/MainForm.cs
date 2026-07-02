namespace FileMaskCleaner.Gui;

public class MainForm : Form
{
    private readonly TextBox _folderBox;
    private readonly TextBox _maskBox;
    private readonly TextBox _targetBox;
    private readonly CheckBox _recursiveCheck;
    private readonly CheckBox _moveCheck;
    private readonly Button _browseFolderButton;
    private readonly Button _browseTargetButton;
    private readonly Button _findButton;
    private readonly Button _actButton;
    private readonly CheckedListBox _filesList;
    private readonly ProgressBar _progressBar;
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
        _browseFolderButton = new Button
        {
            Text = "Обзор…",
            Location = new Point(ClientSize.Width - 92, 11),
            Width = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _browseFolderButton.Click += (_, _) => BrowseInto(_folderBox, "Выберите папку для поиска файлов");

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
            _actButton!.Text = _moveCheck.Checked ? "Переместить отмеченные" : "Удалить отмеченные";
        };

        _findButton = new Button { Text = "Найти", Location = new Point(110, 158), Width = 120, Height = 30 };
        _findButton.Click += async (_, _) => await FindFilesAsync();

        _actButton = new Button
        {
            Text = "Удалить отмеченные",
            Location = new Point(240, 158),
            Width = 180,
            Height = 30,
            Enabled = false,
        };
        _actButton.Click += async (_, _) => await ProcessFilesAsync();

        _progressBar = new ProgressBar
        {
            Location = new Point(430, 162),
            Width = ClientSize.Width - 430 - 12,
            Height = 22,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false,
        };

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
            folderLabel, _folderBox, _browseFolderButton,
            maskLabel, _maskBox, maskHint,
            _recursiveCheck,
            _moveCheck, _targetBox, _browseTargetButton,
            _findButton, _actButton, _progressBar,
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

    private void SetBusy(bool busy)
    {
        _findButton.Enabled = !busy;
        _actButton.Enabled = !busy && _filesList.Items.Count > 0;
        _folderBox.Enabled = !busy;
        _maskBox.Enabled = !busy;
        _recursiveCheck.Enabled = !busy;
        _moveCheck.Enabled = !busy;
        _targetBox.Enabled = !busy && _moveCheck.Checked;
        _browseFolderButton.Enabled = !busy;
        _browseTargetButton.Enabled = !busy && _moveCheck.Checked;
        _filesList.Enabled = !busy;
        _progressBar.Visible = busy;
        UseWaitCursor = busy;
    }

    private async Task FindFilesAsync()
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
        bool recursive = _recursiveCheck.Checked;

        SetBusy(true);
        _progressBar.Style = ProgressBarStyle.Marquee;
        _statusLabel.Text = "Идёт поиск…";
        try
        {
            var files = await Task.Run(() =>
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = recursive,
                    MatchCasing = MatchCasing.CaseInsensitive,
                    IgnoreInaccessible = true,
                };
                return Directory.EnumerateFiles(folder, pattern, options).ToArray();
            });

            _filesList.BeginUpdate();
            _filesList.Items.Clear();
            _filesList.Items.AddRange(files);
            for (int i = 0; i < _filesList.Items.Count; i++)
                _filesList.SetItemChecked(i, true);
            _filesList.EndUpdate();

            _statusLabel.Text = files.Length == 0
                ? $"Файлы по маске «{pattern}» не найдены."
                : $"Найдено файлов: {files.Length}. Снимите галочки с тех, которые нужно оставить.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Ошибка поиска: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Blocks;
            SetBusy(false);
        }
    }

    private async Task ProcessFilesAsync()
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

        SetBusy(true);
        _progressBar.Minimum = 0;
        _progressBar.Maximum = files.Count;
        _progressBar.Value = 0;

        string verb = move ? "Перемещено" : "Удалено";
        var progress = new Progress<int>(done =>
        {
            _progressBar.Value = done;
            _statusLabel.Text = $"{verb} {done} из {files.Count}…";
        });

        var (processed, errors) = await Task.Run(() =>
        {
            var errorList = new List<string>();
            var processedSet = new HashSet<string>();
            int done = 0;
            foreach (var file in files)
            {
                try
                {
                    if (move)
                        File.Move(file, GetUniquePath(target, Path.GetFileName(file)));
                    else
                        File.Delete(file);
                    processedSet.Add(file);
                }
                catch (Exception ex)
                {
                    errorList.Add($"{file}: {ex.Message}");
                }
                done++;
                if (done % 10 == 0 || done == files.Count)
                    ((IProgress<int>)progress).Report(done);
            }
            return (processedSet, errorList);
        });

        // Оставляем в списке только необработанные файлы (одним обновлением, без построчного удаления)
        var remaining = _filesList.Items.Cast<string>().Where(f => !processed.Contains(f)).ToArray();
        _filesList.BeginUpdate();
        _filesList.Items.Clear();
        _filesList.Items.AddRange(remaining);
        _filesList.EndUpdate();

        SetBusy(false);
        _statusLabel.Text = $"{verb}: {processed.Count}, ошибок: {errors.Count}.";
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
