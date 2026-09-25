# TestApp

REST API на .NET 10: разбор HTML-страницы, поиск email, расшифровка AES-256 ECB и запись найденных элементов в PostgreSQL.

## Запуск

```bash
docker compose up --build
```

После старта:

- API: http://localhost:8090
- Swagger: http://localhost:8090/api/swagger
- pgAdmin: http://localhost:8080 (без логина; сервер PostgreSQL уже добавлен)

Единственный метод: `POST /api/parse`. Тела запросов лежат в `json_payload_1.txt` и `json_payload_2.txt`. Эталонные ответы — `json_result_1.txt` и `json_result_2.txt`.

Данные PostgreSQL живут в named volume `pgdata` и сохраняются при перезапуске compose.

## Стек

- ASP.NET Core 10, `System.Text.Json` (UTF-8, indented)
- FluentValidation
- AngleSharp (`ParseDocumentAsync`)
- Dapper + Npgsql, batch `unnest` в транзакции
- source-generated regex (`[GeneratedRegex]`)
- AES-256, `CipherMode.ECB`, `PaddingMode.None` (как в задании)

Контейнер приложения — SDK-образ: исходники монтируются в `/src`, при старте выполняется `dotnet restore/build` и запуск.

## Почему async в этом API

HTTP-запрос держит поток из пула Kestrel, пока метод контроллера не завершится. `async` нужен там, где есть ожидание I/O: сеть до PostgreSQL, разбор большого HTML в AngleSharp, чтение тела запроса. Пока идёт I/O, поток возвращается в пул и может обслуживать другие клиенты.

Осознанный отказ от async:

- регулярные выражения — CPU-bound, в BCL нет `MatchAsync`; `Task.Run` только добавит очередь и аллокации;
- AES `TransformFinalBlock` — короткая синхронная криптография без I/O.

## Структура

Основные границы приложения:

- `Controllers` принимает HTTP-запрос и делегирует сценарий сервису.
- `Services` выполняет валидацию, декодирование, HTML-парсинг, криптографию и запись результата.
- `Models/ParseRequest.cs` и `Models/ParseResponse.cs` содержат контракт API.
- `Models/ParseRequestValidator.cs` содержит правила входных данных.
- `Models/ErrorCodes.cs` содержит стабильные коды ошибок контракта.

Сервис сначала выполняет все проверки и расшифровку, а затем сохраняет найденные элементы. Поэтому невалидный шифротекст не оставляет частично обработанные данные в PostgreSQL.

## Предлагаемая история коммитов

Для чистой демонстрационной истории изменения можно оформить отдельными атомарными коммитами:

1. `chore: add docker and PostgreSQL infrastructure`
2. `feat: add HTML parsing and email extraction`
3. `feat: add AES-256 decryption and API response contract`
4. `feat: persist parsed elements in PostgreSQL`
5. `refactor: split API models and validation rules`
6. `fix: avoid persistence after decryption failure`
7. `docs: describe architecture and local run commands`

Текущий репозиторий уже содержит историю, поэтому эти сообщения являются планом для новой ветки или интерактивного rebase, а не призывом переписывать опубликованный `main`.
