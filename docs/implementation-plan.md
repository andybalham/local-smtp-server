# LocalSmtpCapture Implementation Plan

This plan breaks the initial implementation into small, testable steps. Each step should leave the repository in a buildable state.

## Step 1: Scaffold the Solution

Create the initial .NET solution structure:

- `LocalSmtpCapture.sln`
- `src/LocalSmtpCapture/LocalSmtpCapture.csproj`
- `tests/LocalSmtpCapture.Tests/LocalSmtpCapture.Tests.csproj`

The console app should target .NET 10 LTS and use the package name `LocalSmtpCapture`.

Verification:

- `dotnet build`
- `dotnet test`

## Step 2: Add Configuration Model

Add strongly typed configuration classes for:

- `Smtp:Host`
- `Smtp:Port`
- `Smtp:Authentication:Enabled`
- `Smtp:Authentication:Username`
- `Smtp:Authentication:Password`
- `Storage:OutputFolder`
- `Storage:FolderNamePattern`
- `Console:IncludeRecipients`

Add `appsettings.json` defaults:

- Host: `127.0.0.1`
- Port: `2525`
- Authentication enabled: `true`
- Username: `local`
- Password: `local`
- Output folder: `./emails`
- Include recipients: `true`

Verification:

- Unit test default options binding.
- Unit test invalid port validation.
- Unit test missing or empty output folder validation.

## Step 3: Add Generic Host Startup

Wire the console application through `Microsoft.Extensions.Hosting`.

Startup should:

- Load `appsettings.json`.
- Load environment variables.
- Bind and validate options.
- Log configured SMTP host, port, output folder, and authentication enabled state.

Verification:

- `dotnet build`
- Unit test option validation.
- Manual run confirms startup logs show expected defaults.

## Step 4: Implement Message Folder Naming

Create a small service responsible for generating unique message folder names.

Rules:

- Include local timestamp.
- Include a short unique identifier.
- Produce filesystem-safe folder names.
- Avoid collisions.

Example:

```text
20260615-103000-abc123
```

Verification:

- Unit test folder names contain a timestamp.
- Unit test names are filesystem-safe.
- Unit test repeated calls produce unique names.

## Step 5: Implement Email Persistence

Create a message persistence service that accepts a parsed MIME message and saves:

- `message.eml`
- `body.txt` when a text body exists
- `body.html` when an HTML body exists
- files under `attachments/` when attachments exist

The service should return the saved message folder path and metadata needed for console output.

Verification:

- Unit test plain text email saves `message.eml` and `body.txt`.
- Unit test HTML email saves `message.eml` and `body.html`.
- Unit test multipart email saves both body files.
- Unit test attachment email saves files under `attachments/`.
- Unit test output folder is created when missing.

## Step 6: Implement Console Summary Formatting

Create a formatter for the per-message console summary.

The summary must include:

- Timestamp received.
- Sender.
- Recipients when enabled.
- Recipient count.
- Subject.
- Body availability flags.
- Attachment count and names.
- Saved folder path.

The summary must not include body content or attachment content.

Verification:

- Unit test summary includes sender, recipients, subject, body flags, attachments, and saved path.
- Unit test summary excludes body text.
- Unit test recipient details can be hidden when `Console:IncludeRecipients` is `false`.

## Step 7: Add SMTP Listener

Use the `SmtpServer` package to host the local SMTP endpoint.

The listener should:

- Bind to configured host and port.
- Accept standard SMTP messages.
- Reject or fail authentication when credentials do not match.
- Pass received messages to the persistence service.
- Print the formatted console summary after successful persistence.
- Never relay messages onward.

Verification:

- Unit test authentication validator accepts `local` / `local`.
- Unit test authentication validator rejects invalid credentials.
- Manual run confirms the listener starts on `127.0.0.1:2525`.

## Step 8: Add End-to-End Local Send Test

Add an integration test that starts the SMTP server, sends a test email, and verifies the saved message folder.

The test message should include:

- Sender.
- Recipient.
- Subject.
- Plain text body.

If practical, add a second integration test with:

- HTML body.
- Attachment.

Verification:

- Integration test confirms a message folder is created.
- Integration test confirms `message.eml` is saved.
- Integration test confirms body files are saved.
- Integration test confirms attachments are saved when present.

## Step 9: Graceful Shutdown

Ensure Ctrl+C and host shutdown stop the SMTP listener cleanly.

Verification:

- Manual run starts the server.
- Ctrl+C exits without an unhandled exception.
- Logs show shutdown.

## Step 10: Add Developer Documentation

Add a short `README.md` with:

- Purpose.
- Requirements.
- How to run.
- Default SMTP settings.
- Example client configuration.
- Output folder layout.
- Notes that TLS/STARTTLS and relaying are not included in v1.

Verification:

- README commands work locally.
- Example settings match `appsettings.json`.

## Step 11: Final Validation

Run the full validation set:

- `dotnet build`
- `dotnet test`
- Manual run of the console app.
- Manual send of a sample email to `127.0.0.1:2525` using `local` / `local`.
- Inspect the generated output folder.

Acceptance criteria:

- Server starts with default settings.
- Authenticated local SMTP client can send a message.
- Message is not relayed.
- Unique message folder is created.
- `message.eml` is saved.
- `body.txt` and/or `body.html` are saved when present.
- Attachments are saved under `attachments/`.
- Console summary includes metadata and excludes body content.

