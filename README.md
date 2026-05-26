# Jellyfin ActivityPub Federation Plugin

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Jellyfin Minimum Version](https://img.shields.io/badge/Jellyfin-10.9.0%2B-blueviolet)](https://github.com/jellyfin/jellyfin)

A decentralized, privacy-first federation framework for Jellyfin. This plugin implements the W3C standard ActivityPub protocol directly into the Jellyfin server context, turning isolated instances into an interoperable social media network. Share libraries, follow cross-server friends, and sync playback history across the Fediverse without relying on a centralized corporate cloud.

> ⚠️ **Status: Phase 0 (Implementing Proof of Concept; see: https://github.com/EndlessEden/jellyfin-activitypub/issues/1)
>
> ** Next Stage:  Phase 1 (Proof of Concept)**  
> This project is currently in active development & prototyping. It is built entirely within Jellyfin's decoupled plugin constraints, utilizing localized virtual media namespaces and asynchronous message queues to safeguard core system databases. Aim

---

## Key Features

*   **Federated Identity via WebFinger:** Discover and add friends across instances using a familiar, standardized handle format (`user@domain.tld`) right from the search bar.
*   **Virtual Media Namespacing:** Completely isolates foreign library metadata and delta sync states within a separate, virtual identifier pool (`900000+`), eliminating the risk of database ID collisions or local media schema corruption.
*   **Asynchronous `/inbox` Pipeline:** Employs an isolated SQLite-backed FIFO queue to verify cryptographic HTTP Signatures before ingestion, protecting your server from background network spam and Denial of Service (DoS) exploits.
*   **Capability-Tokenized Streaming:** Issues time-restricted, single-use cryptographic tokens to authorized clients, ensuring media stream validation can bypass local reverse-proxy bottlenecks while maintaining absolute access control.
*   **Outbound Activity Mapping:** Optionally broadcasts active playback metrics as standard ActivityPub `Listen` or `Watch` nodes to connected microblogging systems (e.g., Mastodon, Misskey).

---

## Installation

### Manual Compilation (Development Core)
Because this plugin utilizes custom REST API routes and complex data structures, it must be compiled against your local target SDK:

1. Clone the repository:
```
   bash
   git clone [https://github.com/yourusername/jellyfin-activitypub.git](https://github.com/yourusername/jellyfin-activitypub.git)
   cd jellyfin-activitypub
```
   
2. Compile the binary using the .NET Core SDK:

```
   Bash
   dotnet build --configuration Release
```
   
3. Copy the compiled .dll output artifacts from the build path into your local Jellyfin server directory:

```
   Bash
   cp src/bin/Release/net8.0/Jellyfin.Plugin.ActivityPub.dll /var/lib/jellyfin/plugins/ActivityPub/
```

4. Restart your target Jellyfin system service.

---

Configuration Lifecycle
1. Verification Handshake

    Navigate to Dashboard > Plugins > ActivityPub to access the administration control interface. On initial boot, the plugin generates a localized RSA/Ed25519 identity keypair used to securely sign all outbound message headers.

2. Resolving Remote Vectors

    When a user inputs a query like friend@remote.media into an authorized user profile search bar, the plugin performs an out-of-band lookup routine:

[Local Search] ──> (WebFinger Query) ──> [Remote Server] ──> Returns Actor URL & Public Key


Once identity integrity is confirmed, your instance submits a standardized Follow payload to the remote instance queue and sets the domestic friendship state to Pending Outbound until an Accept activity shifts the context to Connected.
Roadmap

    Phase 1 (Current): Standalone functional plugin pipeline. Complete metadata translation verification via compressed SQLite side-channel deltas, execution of capability tokens, and validation of front-end JavaScript UI dashboard additions.

    Phase 2: Upstream consolidation. Re-architect the core plugin workflows directly into native Jellyfin server source trees, moving internal models to standard schema migrations, and submit an upstream Pull Request to the core project maintainers.

---

Security & Boundary Defenses

    Storage Safeguards: Incoming metadata structures are heavily sanitized and checked against size quotas before local caching to prevent target volume exhaustion.

    Token Isolation: Outbound capability tokens are bound to tight expiration windows and explicitly restricted to single-use media stream playlist handshakes.

    Instant Access Revocation: Processing an Undo or connection block immediately drops active remote player handshakes and purges linked media metadata blocks from the system cache.
