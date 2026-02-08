# 🏦 Financial Web Application API

Backend API для управления личными финансами на .NET 8

## ✨ Возможности
- 🔐 JWT аутентификация и авторизация
- 💰 Управление кошельками и транзакциями
- 👥 CRUD операции для пользователей
- 📊 SQLite база данных с Entity Framework Core
- 🐳 Docker контейнеризация
- 📚 Swagger документация API
- ✅ Модульные тесты (xUnit + Moq)

## 🚀 Быстрый старт

### Требования
- .NET 8 SDK
- Docker (опционально)

### Запуск локально
```bash
# 1. Клонировать репозиторий
git clone https://github.com/shadowfiendd110-code/FinancialWebApplication.git
cd FinancialWebApplication

# 2. Запустить приложение
dotnet run --project FinancialWebApplication


Запуск в Docker

# 1. Собрать образ
docker build -t financial-app .

# 2. Запустить контейнер
docker run -p 8081:80 financial-app

📁 Структура проекта
FinancialWebApplication/
├── Controllers/     # API endpoints
├── Services/        # Бизнес-логика
├── Repositories/    # Работа с данными
├── DTOs/           # Data Transfer Objects
├── Models/         # Entity Framework модели
├── Migrations/     # Миграции БД
└── Tests/          # Модульные тесты

🔐 Аутентификация
Проект использует JWT (JSON Web Tokens). Демонстрационные ключи находятся в appsettings.Production.json.

📚 API Документация
После запуска откройте Swagger UI:
👉 http://localhost:8081/swagger

🛠 Технологии
ASP.NET Core 8 - Web API framework
Entity Framework Core - ORM
SQLite - База данных
JWT Bearer - Аутентификация
xUnit + Moq - Тестирование
Docker - Контейнеризация
Swagger - Документация API
