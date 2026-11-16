# ===== Build =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, so Docker can cache NuGet packages between builds
COPY TodoListTelegramBot/TodoListTelegramBot.csproj TodoListTelegramBot/
RUN dotnet restore TodoListTelegramBot/TodoListTelegramBot.csproj

COPY TodoListTelegramBot/ TodoListTelegramBot/
RUN dotnet publish TodoListTelegramBot/TodoListTelegramBot.csproj \
    -c Release -o /app --no-restore

# ===== Runtime =====
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# DueDateParser uses the local time zone ("jutro 18:00" must mean Polish time)
ENV TZ=Europe/Warsaw

COPY --from=build /app .

# Non-root user provided by the Microsoft images
USER $APP_UID

ENTRYPOINT ["dotnet", "TodoListTelegramBot.dll"]
