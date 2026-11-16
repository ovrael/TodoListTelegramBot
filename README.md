# TodoListTelegramBot

A Telegram bot that connects to a self-hosted [Vikunja](https://vikunja.io/) instance. It lets you browse your Vikunja projects and tasks from a private Telegram chat.

> **Note:** The bot is written for the **Vikunja API v2** (`/api/v2/`). It does not support API v1. The bot adds the `/api/v2/` path to the configured server address itself.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A running Vikunja instance with API v2 (`/api/v2/`)
- A Telegram bot created with [@BotFather](https://t.me/BotFather)

## Configuration

The bot does not keep any secrets in the source code. It reads all values from configuration at startup and stops with an error if a value is missing.

| Key | Environment variable | Required | Description |
|---|---|---|---|
| `Vikunja:Uri` | `Vikunja__Uri` | Yes | Address of the Vikunja server, without the API path. The bot adds `/api/v2/` itself. Example: `https://vikunja.example.com` |
| `Vikunja:Token` | `Vikunja__Token` | Yes | Vikunja API token. Starts with `tk_`. |
| `Telegram:Token` | `Telegram__Token` | Yes | Telegram bot token from @BotFather. Format: `123456789:ABC...` |
| `Telegram:AllowedUserIds` | `Telegram__AllowedUserIds` | Yes | Comma-separated list of numeric Telegram user IDs that can use the bot. Example: `123456789,987654321` |

### How to get the values

**`Vikunja:Uri`**
Use only the address of your Vikunja server (scheme, host and, if necessary, port). Do not add the API path. The bot adds `/api/v2/` itself, so `https://vikunja.example.com` becomes `https://vikunja.example.com/api/v2/`. A trailing slash is optional.

If Vikunja runs under a subpath, include it, for example `https://example.com/vikunja`.

If the address already contains `/api/`, the bot stops at startup with an error. At startup the bot writes the full API address to the console (`Vikunja API address: ...`).

**`Vikunja:Token`**
1. In Vikunja, open **Settings → API Tokens**.
2. Create a new token. Give it only the permissions the bot needs (projects and tasks) and set an expiry date.
3. Copy the token. Vikunja shows it only once.

**`Telegram:Token`**
1. Open a chat with [@BotFather](https://t.me/BotFather).
2. Send `/newbot` and follow the steps.
3. Copy the token. If the token leaks, send `/revoke` to BotFather to get a new one.

**`Telegram:AllowedUserIds`**
The bot ignores messages from users who are not on this list. It also ignores all messages that are not in a private chat.

To find your user ID:
1. Set the value to `0` and start the bot.
2. Send any message to the bot.
3. Find the line `Ignored message from unauthorized user <ID>` in the console output.
4. Put this ID in the configuration and restart the bot.

## Setting the values

### Local development: User Secrets

User Secrets are stored outside the project directory (`~/.microsoft/usersecrets/`), so they never get into the repository.

```bash
cd TodoListTelegramBot
dotnet user-secrets set "Vikunja:Uri" "https://vikunja.example.com"
dotnet user-secrets set "Vikunja:Token" "tk_..."
dotnet user-secrets set "Telegram:Token" "123456789:ABC..."
dotnet user-secrets set "Telegram:AllowedUserIds" "123456789"
```

To see the current values:

```bash
dotnet user-secrets list
```

### Server, Docker or systemd: environment variables

Use a double underscore (`__`) instead of a colon (`:`) in the key names:

```bash
Vikunja__Uri=https://vikunja.example.com
Vikunja__Token=tk_...
Telegram__Token=123456789:ABC...
Telegram__AllowedUserIds=123456789
```

- **systemd:** put the values in a file (for example `/etc/todolisttelegrambot.env`), set `chmod 600` on it and point to it with `EnvironmentFile=`.
- **Docker Compose:** use `env_file:` or `secrets:`.

Never commit `.env` files. The `.gitignore` in this repository already excludes them.

## Running

```bash
dotnet run --project TodoListTelegramBot
```

## Deployment with Docker Compose (next to Vikunja)

The bot uses long polling. It does not need open ports or a reverse proxy. It only needs outgoing access to `api.telegram.org` and access to the Vikunja API.

1. On the Docker host (for example, the LXC container with Vikunja), clone the repository:
   ```bash
   git clone <repo-url> /opt/todolist-telegram-bot
   cd /opt/todolist-telegram-bot
   ```
2. Find the name of the Vikunja network and the Vikunja service name:
   ```bash
   docker network ls          # for example: vikunja_default
   docker compose -f <vikunja-compose-file> config --services
   ```
   If the network name is different, change `name:` in `docker-compose.yml`.
3. Create the `.env` file and fill in the values:
   ```bash
   cp .env.example .env
   chmod 600 .env
   ```
   Inside the Docker network, use the Vikunja service name and the internal port, without the API path, for example `http://vikunja:3456`. The bot adds `/api/v2/` itself.
4. Build and start the bot:
   ```bash
   docker compose up -d --build
   docker compose logs -f
   ```

To update the bot:

```bash
git pull && docker compose up -d --build
```

## Bot commands

| Command | Description |
|---|---|
| `/projects` | List Vikunja projects |
| `/tasks` | List tasks from newest to oldest |
| `/tasks-newest` | List tasks from oldest to newest |

## Security notes

- Keep all four values out of the source code and out of the repository.
- If a token leaks, revoke it at once (BotFather `/revoke`, or delete the token in Vikunja) and create a new one.
- If the bot and Vikunja run on different machines, use HTTPS for `Vikunja:Uri`. Over plain HTTP the Vikunja token is sent as clear text.
