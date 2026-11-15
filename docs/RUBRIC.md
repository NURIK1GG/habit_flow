# Соответствие рубрике из Final Requirements CSh.docx

Документ — справочный источник критериев. Окончательная оценка и защита зависят от преподавателя; эта таблица указывает проверяемые реализации.

| № | Критерий, 5% | Доказательство в проекте |
|---|---|---|
| 1 | Code Structure and Design | Четыре проекта, интерфейсы и DI; ARCHITECTURE.md |
| 2 | Component Reusability | RoutineCard, StatCard, ProgressChart, ModalShell; Parameter, CascadingParameter, EventCallback; bUnit |
| 3 | Cross-Platform Compatibility | net8.0, SQLite, локальные vendor-файлы; общие команды запуска. Фактический запуск проверен на Linux; Windows/macOS требуют проверки на соответствующих хостах |
| 4 | State Management | Scoped AppState, Changed, SemaphoreSlim, отмена lifetime; StateTests |
| 5 | UI Design | Согласованная палитра, навигация, dashboard, карточки, SVG-график |
| 6 | UX | Чёткие пустые состояния, тосты, rest days, undo, archive/restore, фокус модального окна |
| 7 | Responsiveness | CSS grids/media queries; E2E desktop/tablet/mobile без горизонтального overflow |
| 8 | Core Functionality | Builder, упражнения, check-in, серии, личный журнал, диаграммы, opt-in рейтинг |
| 9 | Advanced Features | SignalR, Identity RBAC, security stamps, concurrency token |
| 10 | Error Handling and Resilience | ErrorBoundary, StateBanner, серверная валидация, уникальный индекс, обработка конфликтов и reconnect |
| 11 | Efficiency | Async EF, factory contexts, no-tracking reads, SQL aggregation, Virtualize, SSR |
| 12 | Scalability | Разделённые порты, индекс владельца/даты, bounded ranking; эксплуатационные пределы SQLite описаны явно |
| 13 | Resource Utilization | Dispose/IAsyncDisposable, отмена загрузок, debounced SignalR, отсутствие polling |
| 14 | Testing Coverage | xUnit rules/state, SQLite integration, bUnit, HTTP TestServer, Playwright пользовательские сценарии |
| 15 | Quality Assurance | QA.md с реально запущенными проверками; axe и браузерные viewport-сценарии |
| 16 | Documentation | README, схема БД, UML, user guide, test credentials, developer/production notes |
| 17 | Innovation | Единые привычки/тренировки, серии с rest days, равные очки без стимула завышать минуты, добровольная публичность |
| 18 | Creative Blazor/C#/.NET | Native-dialog JS interop, SVG-компонент, Virtualize, TimeProvider, events, optimistic concurrency |
| 19 | Professionalism | Zero-warning build, миграции, seed с разделением окружений, security checks, воспроизводимые тесты |
| 20 | Presentation | DEFENSE.md: план 15–20 минут, демонстрация, вопросы, технологии и методология |

Организационные условия документа: индивидуальная работа либо команда максимум из двух студентов, оба программируют; защита обязательна, каждый участник должен понимать все части проекта. В коде нет возможности автоматически подтвердить участие или провести защиту; эти условия выполняют авторы при сдаче.
