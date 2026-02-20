# NoteVault

Веб-приложение на ASP.NET Core для:

- регистрации пользователя с защитой от ботов;
- авторизации через cookie;
- создания и редактирования заметок;
- загрузки, скачивания и удаления файлов.

## Функции безопасности

- Хеширование паролей (PBKDF2 + соль).
- Антибот при регистрации:
  - honeypot-поле;
  - простая арифметическая капча;
  - проверка минимального времени заполнения формы.
- Доступ к заметкам и файлам только после авторизации.
- Файлы доступны только владельцу.

## Технологии

- .NET 9
- ASP.NET Core Razor Pages
- EF Core + SQLite
- Docker

## Запуск

```bash
dotnet run
```

## Сборка

```bash
dotnet build
```

## Где хранятся данные

- Локально по умолчанию:
  - База данных SQLite: `data/app.db`
  - Загруженные файлы: `data/files/<userId>/...`

Путь можно изменить переменной окружения `AppDataRoot`.

## Продакшн деплой (Render)

GitHub Pages не умеет запускать ASP.NET Core сервер, поэтому для этой версии нужен серверный хостинг.

1. Откройте кнопку деплоя:

   [![Deploy to Render](https://render.com/images/deploy-to-render-button.svg)](https://render.com/deploy?repo=https://github.com/DigilevichAlexandr/WordLearnerKids)

2. Подтвердите создание web service из `render.yaml`.
3. Дождитесь первого билда и получите URL вида `https://<service>.onrender.com`.

Что уже настроено:

- Docker-сборка (`Dockerfile`);
- healthcheck endpoint: `/health`;
- постоянный диск `/var/data` для SQLite, файлов и ключей cookie;
- автодеплой при изменениях в `master`.
