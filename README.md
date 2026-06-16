# LocalSmtpCapture

LocalSmtpCapture is a local SMTP capture server for development and automated testing. It accepts email from applications under test, prevents outbound delivery, prints a concise console summary, and saves each received message to disk for inspection.

## Requirements

- .NET 10 SDK
- A local SMTP client or application that can send to an unencrypted SMTP endpoint

## Run

From the repository root:

```powershell
dotnet run --project src\LocalSmtpCapture\LocalSmtpCapture.csproj
```

The default listener starts on `127.0.0.1:2525` and requires SMTP authentication with username `local` and password `local`.

## Configuration

Defaults are stored in `src/LocalSmtpCapture/appsettings.json` and can be overridden with environment variables.

| Setting | Default |
| --- | --- |
| `Smtp:Host` | `127.0.0.1` |
| `Smtp:Port` | `2525` |
| `Smtp:Authentication:Enabled` | `true` |
| `Smtp:Authentication:Username` | `local` |
| `Smtp:Authentication:Password` | `local` |
| `Storage:OutputFolder` | `./emails` |
| `Storage:Retention:PruneCapturedMessages` | `true` |
| `Storage:Retention:MaxMessages` | `30` |
| `Console:IncludeRecipients` | `true` |

Example PowerShell override:

```powershell
$env:Smtp__Port = "2526"
$env:Storage__OutputFolder = "C:\tmp\captured-email"
$env:Storage__Retention__MaxMessages = "100"
dotnet run --project src\LocalSmtpCapture\LocalSmtpCapture.csproj
```

## Example Client Settings

Configure the application under test with:

```text
SMTP URL: smtp://local:local@127.0.0.1:2525
SMTP host: 127.0.0.1
SMTP port: 2525
Username: local
Password: local
TLS/STARTTLS: disabled
Authentication: enabled
```

If the client expects a URL without credentials, use:

```text
smtp://127.0.0.1:2525
```

Then configure `local` / `local` separately as the SMTP username and password. Some clients label this value as the SMTP server, mail server, relay host, or connection string rather than URL.

Messages are captured locally only. They are not relayed to external mail servers.

## Output Layout

Each accepted message is saved under a unique, filesystem-safe folder in `Storage:OutputFolder`.

```text
emails/
  20260615-103000-abc123/
    message.eml
    body.txt
    body.html
    attachments/
      invoice.pdf
```

`message.eml` is always written. `body.txt`, `body.html`, and `attachments/` are written when the message contains matching content.

## Retention

Captured message pruning is enabled by default. After each successfully persisted message, LocalSmtpCapture keeps the newest `Storage:Retention:MaxMessages` captured message folders and removes older captured message folders from `Storage:OutputFolder`.

Only folders that contain `message.eml` are considered captured message folders. Set `Storage:Retention:PruneCapturedMessages` to `false` to disable automatic pruning, or increase `Storage:Retention:MaxMessages` to retain more captures.

## Console Summary

For each captured email, the console logs metadata such as the received timestamp, sender, recipient count, recipients when enabled, subject, body availability, attachment names, and saved folder path. Message body content and attachment contents are not printed.

## Validation

Run the standard checks from the repository root:

```powershell
dotnet build LocalSmtpCapture.sln
dotnet test LocalSmtpCapture.sln
dotnet run --project src\LocalSmtpCapture\LocalSmtpCapture.csproj --no-build
```

The final command starts the SMTP listener and keeps running until you stop it with Ctrl+C.

## Version 1 Notes

- TLS and STARTTLS are not included.
- Message relaying is not included.
- A web UI is not included.
- Mailbox and user management are not included.
- Captured message retention is count-based. Time-based cleanup is not included.
