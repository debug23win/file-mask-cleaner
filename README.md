# FileMaskCleaner

Утилита на C# (.N
## Выпуск новой версии — целиком на GitHub

Локальный компьютер не нужен: сборкой exe занимается GitHub Actions
([release.yml](.github/workflows/release.yml)).

1. Отредактируйте код прямо на GitHub (кнопка ✏️ в файле или клавиша `.` для веб-редактора).
2. Откройте **Actions → Release → Run workflow**, укажите тег новой версии (например `v1.3.0`)
   и запустите. Через пару минут готовый `FileMaskCleaner.exe` появится в
   [Releases](https://github.com/debug23win/file-mask-cleaner/releases).

Альтернатива: просто создайте тег `v*` (или релиз с новым тегом) — workflow соберёт exe
и прикрепит его к релизу автоматически. Запуск workflow без тега делает проверочную
сборку без публикации релиза (exe остаётся артефактом сборки).
