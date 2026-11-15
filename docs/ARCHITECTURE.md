# Архитектура и UML

## Направление зависимостей

```mermaid
classDiagram
    class Routine {
        +Guid Id
        +string OwnerId
        +int DaysOfWeek
        +int Revision
        +IsScheduled(DateOnly) bool
    }
    class RoutineStep {
        +string Name
        +int Sets
        +int Repetitions
    }
    class CheckIn {
        +DateOnly Date
        +int Minutes
        +string Notes
    }
    class StreakCalculator {
        +Current(Routine, DateOnly) int
        +Best(Routine, DateOnly) int
    }
    class ITrackerService {
        <<interface>>
        +LoadAsync()
        +SaveRoutineAsync()
        +CheckInAsync()
        +UndoAsync()
    }
    class TrackerService
    class IRoutineRepository {
        <<interface>>
        +ListAsync()
        +SaveAsync()
        +AddCheckInAsync()
        +LeadersAsync()
    }
    class RoutineRepository
    class AppState {
        +DashboardDto Data
        +bool IsBusy
        +string Error
        +event Changed
        +RefreshAsync()
        +MutateAsync()
    }
    Routine "1" *-- "0..20" RoutineStep
    Routine "1" *-- "0..*" CheckIn
    StreakCalculator ..> Routine
    TrackerService ..|> ITrackerService
    RoutineRepository ..|> IRoutineRepository
    TrackerService --> IRoutineRepository
    TrackerService --> StreakCalculator
    AppState --> ITrackerService
```

## Успешная отметка и синхронизация

```mermaid
sequenceDiagram
    actor User
    participant Card as RoutineCard
    participant UI as Workspace / CheckInEditor
    participant State as AppState
    participant App as TrackerService
    participant Auth as ICurrentUser
    participant Repo as RoutineRepository
    participant DB as SQLite
    participant Hub as SignalR
    participant Other as Other browser
    User->>Card: Check in
    Card->>UI: EventCallback(RoutineDto)
    UI->>State: MutateAsync(command)
    State->>App: CheckInAsync(dto)
    App->>Auth: GetIdAsync / fresh security checks
    App->>App: DataAnnotations + schedule + date rules
    App->>Repo: AddCheckInAsync(ownerId, entry)
    Repo->>DB: INSERT + revision UPDATE in SaveChanges transaction
    DB-->>Repo: unique index + concurrency verified
    App->>Hub: ProgressChanged (no private payload)
    Hub-->>Other: invalidate
    Other->>App: Load own authorized dashboard
    State->>App: LoadAsync
    State-->>UI: Changed
    UI-->>User: Updated card + toast
```

## SOLID и время жизни

| Принцип | Реализация |
|---|---|
| SRP | StreakCalculator вычисляет серии, TrackerService выполняет сценарии, Repository сохраняет, компоненты рисуют |
| OCP | IProgressNotifier/IRoutineRepository можно заменить без изменения сценариев |
| LSP | Реализации интерфейсов соблюдают единый контракт DTO/исключений |
| ISP | Отдельные ITrackerService, IAdminService, ICurrentUser, IProgressNotifier |
| DIP | Application зависит от портов, реализации подключаются DI в Program.cs |

AppState и ToastService — scoped, изолированы на Blazor circuit. TimeProvider и потокобезопасный SignalR publisher — singleton. Репозиторий создаёт DbContext на операцию. Клиентские подписки снимаются в Dispose/IAsyncDisposable; native dialog возвращает фокус, ловит Escape и ограничивает клавиатурный фокус. Журнал использует Virtualize.

## Решения и компромиссы

- Interactive Server уменьшает клиентский bundle и держит работу с БД на сервере; сеть нужна для действий пользователя.
- Свой SVG проще тяжёлой библиотеки для одной временной серии; таблица сохраняет доступность точных данных.
- Расписание фиксируется после первой отметки: изменение сегодня не переписывает исторические ожидания.
- Очки равны 10 за запись: введённые минуты не дают преимущества в соревновании.
- События SignalR сообщают лишь факт изменения. Каждый клиент читает собственные данные через проверку владельца. На большом числе клиентов широковещательную инвалидизацию следует заменить группами и отдельной инвалидизацией публичного рейтинга.
- SQLite поддерживает реальную уникальность и транзакции без отдельной установки; ревизия целочисленная, поскольку SQLite не имеет SQL Server rowversion.
- `SaveChangesAsync` атомарно изменяет ревизию программы и запись. Конкурентный архив/редактирование вызывает конфликт, дубль даты — ошибку ограничения. Автоматический повтор записи без проверки не выполняется.
- Удаление программы заменено обратимым архивом, чтобы сохранять историю. Исправление ежедневной записи выполняется Undo и повторным check-in.
