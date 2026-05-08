# SecureFileTransfer

A cross-platform peer-to-peer file transfer tool built in C# (.NET 9). Transfers files directly between machines on a local network with end-to-end encryption — no cloud, no external services, no file size limits.

Available as both a **CLI** and a **GUI** (Avalonia, currently in development).

---

## Security Model

Every transfer is protected by a full cryptographic handshake:

| Layer | What's used |
|---|---|
| Key exchange | ECDH on NIST P-256 — a fresh ephemeral keypair per session |
| Key derivation | `DeriveKeyFromHash` with SHA-256 (no raw ECDH output is used directly) |
| Encryption | AES-256-GCM — authenticated encryption with per-chunk counter nonces |
| Authentication | TOFU (Trust On First Use) — peer public key fingerprints are stored and verified on every subsequent connection |

### TOFU Key Pinning

On the **first** connection to a peer, the remote party's public key fingerprint (SHA-256 of their ECDH public key) is saved to your local peer config. Every time you connect to that peer after that, the fingerprint is verified against the stored value. If it doesn't match, the connection is immediately aborted with a warning — this is the indicator of a potential man-in-the-middle attack.

This is the same model SSH uses for host key verification.

---

## Features

- Peer-to-peer file transfer over TCP — direct, no relay
- End-to-end encrypted with AES-256-GCM
- ECDH key exchange (NIST P-256) with a fresh keypair per session
- TOFU fingerprint pinning to detect MITM attacks
- Multi-file transfer in a single session
- Persistent peer list with fingerprint storage
- Transfer history and debug logging
- Cross-platform: Windows, macOS, Linux
- CLI (stable) + GUI via Avalonia (in development)

---

## How It Works

1. One machine starts in **host mode** and listens for incoming connections
2. Another machine starts in **client mode** and connects to the host by IP
3. A plaintext handshake exchanges machine names and IPs; both sides auto-save new peers
4. An ECDH key exchange establishes a shared AES-256 session key
5. TOFU verification checks the remote party's public key fingerprint
6. Files are transferred as AES-GCM encrypted chunks over the TCP stream
7. Transfer results are logged locally on both sides

---

## Project Structure

```
SecureFileTransfer/
├── Program.cs
├── src/
│   ├── client/          # CLI_ClientService, GUI_ClientService
│   ├── host/            # CLI_HostService, GUI_HostService, ManagePeers
│   ├── protocols/       # Handshake, KeyExchange, FileInfo, TransferPlan, FileTransfer
│   ├── security/        # EncryptionService, KeyExchangeService, SessionKeyModel
│   ├── setup/           # HostConfigManager, AppPaths, Initialize, config managers
│   ├── logging/         # DebugLogger, TransferLogging, TransferLoggingManager
│   ├── helper/          # MessageHelper (length-prefixed framing), FileBrowserService
│   └── data_structures/ # HostModel, PeersModel, ConnectionLogModel, etc.

SecureFileTransfer.Gui/
├── App.axaml
├── ViewModels/          # MainWindowViewModel (CommunityToolkit.Mvvm)
└── Views/               # MainWindow.axaml
```

---

## Data Storage

Configuration and logs are stored locally — nothing leaves your machine except the encrypted file transfer itself.

| Platform | Location |
|---|---|
| Windows | `%AppData%\SecureFileTransfer\` |
| macOS / Linux | `~/.securefiletransfer/` |

Files stored:
- `host.yaml` — your machine name, IP addresses, port, and saved peers (with fingerprints)
- `transfer_logs.yaml` — history of all completed transfers
- `download_path.txt` — last used download directory
- `debug.log` — timestamped debug log

IP addresses (IPv4 and IPv6) are automatically refreshed from your network interfaces on every startup.

---

## Running

**From source:**
```
dotnet run
```

**Published builds** (in `/publish`):
```
./sft              # macOS / Linux
sft.exe            # Windows
```

**Optional — install to PATH:**

macOS / Linux:
```bash
chmod +x sft
sudo mv sft /usr/local/bin/sft
```

Windows: add the folder containing `sft.exe` to your system PATH.

---

## Networking

- Protocol: TCP with custom length-prefixed message framing
- Default port: `5000` (configurable in `host.yaml`)
- Currently supports IPv4 local network connections
- IPv6 support is in progress

---

## Development Status

| Area | Status |
|---|---|
| CLI — core transfer | Stable |
| CLI — peer management | Stable |
| CLI — encrypted transfer | Stable |
| TOFU key pinning | Implemented |
| GUI (Avalonia) — host/receive | In progress |
| GUI (Avalonia) — client/send | In progress |
| IPv6 cross-network transfers | Planned |
| Interrupted transfer resume | Planned |

---

## Author

Arihant Singh
