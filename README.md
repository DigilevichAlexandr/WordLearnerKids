# WordLearnerKids

Простое Blazor WebAssembly приложение для детей: показывает случайное слово и переключает его по кнопке.

## Стек

- .NET 9
- Blazor WebAssembly
- Bootstrap (базовые стили)

## Запуск локально

```bash
dotnet run
```

## Сборка

```bash
dotnet build
```

## Автопубликация в GitHub Pages

В проекте есть workflow `.github/workflows/deploy-pages.yml`.

Как включить:

1. Открой `Settings -> Pages`.
2. В блоке `Build and deployment` выбери `Source: GitHub Actions`.
3. После merge в `master` деплой запустится автоматически.

Если нужно запустить вручную:

- вкладка `Actions` -> workflow `Deploy to GitHub Pages` -> `Run workflow`.

Адрес после публикации обычно:

- `https://<username>.github.io/<repo>/`
