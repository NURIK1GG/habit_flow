# Загрузка в GitHub двумя участниками

Оба участника используют собственные GitHub-аккаунты и собственные коммиты. Это инструкция для настоящей совместной работы: распределение загрузки готовых файлов само по себе не доказывает участие в программировании. Каждый должен самостоятельно изучить, изменить и проверить свою часть и уметь объяснить её на защите.

## Распределение

| Участник 1: сервер и данные | Участник 2: интерфейс и проверки |
|---|---|
| src/HabitFlow.Domain | src/HabitFlow.Web |
| src/HabitFlow.Application | tests/HabitFlow.Tests |
| src/HabitFlow.Infrastructure | tests/e2e |
| HabitFlow.sln, global.json, Directory.Build.props | .github/workflows/ci.yml |
| .gitignore, .config/dotnet-tools.json | README.md, THIRD-PARTY-NOTICES.md |
| docs/DATABASE.md, docs/ARCHITECTURE.md | остальные материалы docs |

Примеры содержательной личной работы: участник 1 дорабатывает доменные правила/запросы и проверяет миграции; участник 2 дорабатывает формы, тему и тестирует пользовательские сценарии. Не подменяйте автора чужим email и не создавайте фиктивную историю коммитов.

## 1. Подготовка

Установите Git и .NET 8 SDK. Участник 1 создаёт пустой репозиторий в GitHub: New repository → название HabitFlow → Create repository. Автоматическое создание README/.gitignore выключить.

Settings → Collaborators → Add people: пригласить участника 2. Он принимает приглашение. Каждый на своём компьютере задаёт в Git СВОИ имя и email, привязанный к GitHub (можно использовать GitHub noreply email из Settings → Emails):

```sh
git config --global user.name "Ваше настоящее имя"
git config --global user.email "Ваш email из GitHub"
```

## 2. Участник 1 загружает серверную часть

Распаковать архив. Открыть терминал в папке с HabitFlow.sln:

```sh
git init -b main
git add .gitignore global.json Directory.Build.props HabitFlow.sln .config
git add src/HabitFlow.Domain src/HabitFlow.Application src/HabitFlow.Infrastructure
git add docs/DATABASE.md docs/ARCHITECTURE.md
git commit -m "Add domain, application services and SQLite infrastructure"
```

На странице созданного репозитория скопировать команду `git remote add origin ...` со СВОЕЙ ссылкой и выполнить её. Затем:

```sh
git push -u origin main
```

Это промежуточная загрузка: решение ссылается на Web и Tests, которые появятся на следующем шаге. Полная сборка выполняется после объединения обеих частей.

## 3. Участник 2 загружает интерфейс и тесты

Клонировать репозиторий через Code → HTTPS → скопировать ссылку. В терминале выполнить `git clone` с этой ссылкой, затем открыть полученную папку HabitFlow.

```sh
git switch -c feature/ui-and-tests
```

Из распакованного архива скопировать ВНУТРЬ клонированного репозитория:

- папку src/HabitFlow.Web;
- папку tests;
- папку .github;
- README.md и THIRD-PARTY-NOTICES.md;
- остальные документы из docs.

Не копировать папку .git, bin, obj, node_modules или App_Data. Проверить, что папки не вложились дважды: правильный путь — `src/HabitFlow.Web/HabitFlow.Web.csproj`.

Внести и проверить свои изменения, затем:

```sh
dotnet restore HabitFlow.sln
dotnet build HabitFlow.sln
dotnet test HabitFlow.sln
git add src/HabitFlow.Web tests .github README.md THIRD-PARTY-NOTICES.md docs
git commit -m "Add Blazor interface, theme switch and automated tests"
git push -u origin feature/ui-and-tests
```

В GitHub нажать Compare & pull request → Create pull request. Участник 1 проверяет изменения и объединяет PR. Выбрать **Create a merge commit**, чтобы сохранить отдельные коммиты и их авторов.

## 4. Продолжение работы

Оба выполняют `git switch main`, затем `git pull`. Для каждого следующего изменения создавать отдельную ветку, делать свои коммиты и PR. Не редактировать один файл одновременно без согласования. После объединения запустить CI и проверить Contributors/историю коммитов.

## Через Visual Studio

Открыть HabitFlow.sln. Команды можно выполнять в View → Terminal. Встроенное окно Git Changes позволяет просмотреть изменения, написать commit message и нажать Push. Переключение/создание ветки доступно в меню Git. При первом Push войти в СВОЙ GitHub-аккаунт.

Рабочая SQLite-база и ключи находятся в App_Data и исключены через .gitignore. На компьютере второго участника база создастся автоматически при запуске.
