# Local SMTP Test Server Requirements

## Purpose

Build a local SMTP server for development and automated testing. The server accepts email from applications under test, prevents outbound delivery, prints a useful console summary, and persists received email content to a configured local folder for inspection.

## Goals

- Provide a .NET console application that runs locally.
- Listen for SMTP traffic on a configurable host and port.
- Accept test emails without forwarding them to real recipients.
- Write a concise summary of each received message to the console.
- Save each received email to a configured folder.
- Make behavior easy to configure for local developer machines.
- Fail clearly when configuration is invalid or the output folder is unavailable.

## Non-Goals

- No relay to external SMTP servers.
- No production mail delivery guarantees.
- No web UI in the first version.
- No mailbox/user management in the first version.
- No persistence database in the first version.

## Target Users

- Developers testing email-producing application flows.
- Automated test suites that need a local SMTP endpoint.
- QA or support engineers reproducing email scenarios locally.

## Functional Requirements

### Server Startup

- The application must start from the command line as a .NET console app.
- The application and package name must be `LocalSmtpCapture`.
- The server must bind to a configured host and port.
- On startup, the console must display:
  - SMTP host.
  - SMTP port.
  - Email output folder.
  - Current environment/configuration source, if practical.

### SMTP Message Acceptance

- The server must accept standard SMTP messages from local clients.
- The server must support SMTP authentication for clients that require credentials.
- The default local SMTP username must be `local`.
- The default local SMTP password must be `local`.
- The server must support at least:
  - Sender address.
  - One or more recipient addresses.
  - Subject.
  - Plain text body.
  - HTML body, when provided.
  - Attachments, if the underlying message parser supports them cleanly.
- The server must not send messages onward.

### Console Summary

For every received email, the console must print a summary including:

- Timestamp received.
- Sender.
- Recipient count and recipient addresses.
- Subject.
- Whether the message has plain text, HTML, and attachments.
- Attachment count and basic attachment names when attachments are present.
- Saved file path.

The summary should be readable in a normal terminal and avoid dumping full email bodies by default.

### Email File Logging

- The server must save each received email to the configured folder.
- Each received email must be saved into a unique, filesystem-safe message folder.
- The message folder name should include a timestamp and a short unique identifier.
- The server must always save the raw message as `message.eml`, because it preserves headers, body parts, and attachments.
- When a plain text body exists, the server must save it as `body.txt`.
- When an HTML body exists, the server must save it as `body.html`.
- If both plain text and HTML bodies exist, the server must save both.
- Attachments must be saved under an `attachments` subfolder.
- The server must create the output folder if it does not exist.
- If saving fails, the server must log the failure clearly to the console.

Expected folder shape:

```text
emails/
  20260615-103000-abc123/
    message.eml
    body.txt
    body.html
    attachments/
      invoice.pdf
```

### Configuration

The application must support configuration through `appsettings.json` and environment variables.

Required configuration:

- `Smtp:Host`
- `Smtp:Port`
- `Smtp:Authentication:Enabled`
- `Storage:OutputFolder`

Suggested defaults:

- `Smtp:Host`: `127.0.0.1`
- `Smtp:Port`: `2525`
- `Smtp:Authentication:Enabled`: `true`
- `Smtp:Authentication:Username`: `local`
- `Smtp:Authentication:Password`: `local`
- `Storage:OutputFolder`: `./emails`

Optional configuration:

- `Logging:LogLevel`
- `Storage:FolderNamePattern`
- `Console:IncludeRecipients`

### Shutdown

- The server must handle Ctrl+C gracefully.
- On shutdown, the server should stop accepting new SMTP connections and flush any pending writes where possible.

## Non-Functional Requirements

### Platform

- The application should target .NET 10 LTS.
- The application should run on Windows, macOS, and Linux.

### Reliability

- A malformed email should not crash the process.
- One failed message write should not stop the server.
- Startup should fail fast for invalid host, port, or output folder configuration.

### Security

- The server is intended for local development and testing only.
- The default bind address should be loopback, not all interfaces.
- The output folder must be treated as local test data and may contain sensitive email content.
- Authentication is required for the first version.
- TLS/STARTTLS is not required for the first version unless a target client proves it is needed.

### Observability

- Console logs should be enough for local use.
- Log messages should clearly distinguish startup, received message, save success, save failure, and shutdown events.

### Testability

- Core message handling and file persistence should be testable without opening a real socket.
- Tests should verify that a sent message is accepted and saved to disk.
- CI-specific dynamic port behavior is not required for the first version.

## Proposed Technical Direction

- Use a .NET console application with the generic host (`Microsoft.Extensions.Hosting`) for configuration, logging, and lifetime management.
- Use the `SmtpServer` library for the SMTP listener.
- Use `MimeKit` for parsing, writing, and extracting email content.
- Keep persistence as filesystem-only for the first version.
- Do not add automatic cleanup or retention behavior in the first version.

## Decisions

- Target framework: .NET 10 LTS.
- Project/package name: `LocalSmtpCapture`.
- SMTP authentication: required for the first version.
- SMTP authentication defaults: username `local`, password `local`.
- TLS/STARTTLS: leave out of version 1 unless a target client requires it.
- Bind address: default to `127.0.0.1`; allow explicit configuration to `0.0.0.0` when needed.
- Attachment console metadata: include attachment count and names, but never attachment contents.
- Attachment extraction: save attachments under the message folder's `attachments` subfolder.
- Console recipients: show all recipients by default.
- Configuration sources: support `appsettings.json` and environment variables for version 1.
- Library choices: use `SmtpServer` and `MimeKit`.
- `summary.json`: not included in the first version.
- Console body preview: not included; the console should avoid email body content.
- Cleanup/retention: not included; cleanup will be manual.
- CI support: not a first-class scenario; the first version is for local debugging.

## Decision Notes

### TLS/STARTTLS

TLS encrypts the SMTP connection from the client to this local server. STARTTLS starts as a normal SMTP connection and then upgrades it to TLS after the client asks.

For a local debugging server, TLS is usually unnecessary when the application under test can send to plain SMTP on `127.0.0.1`. It becomes useful when the client library, framework, or application configuration requires secure SMTP and cannot be relaxed for local debugging.

Decision for version 1: leave TLS/STARTTLS out unless a target client requires it.

### Bind Address

Binding to `127.0.0.1` means only software on the same machine can connect. This is safer and is the right default for local debugging.

Binding to `0.0.0.0` means other machines on the network can connect to the SMTP server. This can be useful for testing from a phone, VM, container, or another machine, but it also exposes captured email behavior to the local network.

Decision for version 1: default to `127.0.0.1`, allow configuration to `0.0.0.0` only when explicitly needed.

### Attachment Console Metadata

Example console summary:

```text
Received email 2026-06-15 10:30:00 +01:00
From: sender@example.com
To: recipient@example.com
Subject: Invoice test
Bodies: text=yes html=yes
Attachments: 2 (invoice.pdf, terms.txt)
Saved: C:\emails\20260615-103000-abc123
```

The console should show attachment names and counts, but not attachment contents.

## Initial MVP Scope

The first build should include:

- Console app project.
- Project/package name `LocalSmtpCapture`.
- .NET 10 LTS target framework.
- Configurable SMTP host, port, and output folder.
- Local SMTP listener with authentication support.
- Receive message callback.
- Save each received email into a unique message folder.
- Save raw `message.eml`.
- Save extracted `body.txt` and/or `body.html` when those body parts exist.
- Save attachments under `attachments/`.
- Print one structured summary per received email.
- Graceful Ctrl+C shutdown.
- Unit tests for filename generation and message persistence.
- Integration test that sends one test email and verifies the saved message folder contents.
