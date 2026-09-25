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

Минимальный набор по условию задания: контроллер, сервис, модели, плюс инфраструктура docker/pgadmin.
