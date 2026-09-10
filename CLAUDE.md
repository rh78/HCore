> **Read the workspace root config first.** This repo is one of ~46 that build together.
> `.claude/CLAUDE.md` in the Claude Code config repo checked out at the **workspace root**
> (the folder containing `HCore/` and `Smint.io/`) carries two things you need: the
> **workspace map** — what each repo is, how they depend on each other, and the OpenAPI
> code-generation workflow — and the **working agreements** that govern how changes are
> made here. Read it before making cross-repo changes.
>
> Note: cross-repo dependencies are **relative `ProjectReference` paths**, so every
> repo must be cloned side by side in the standard layout or this repo will not build.

# HCore

The shared ASP.NET Core foundation both product lines are built on — "an opinionated
collection of projects that help you build great ASP.NET Core projects" (README), MIT
licensed. 20 projects, all `net8.0`, SDK pinned to `8.0.101` by `global.json`.

Two things make this repo different from every other one in the workspace.

## 1. Blast radius: it is consumed as source, not as a package

Consumers reach HCore through **relative `ProjectReference` paths**, not NuGet. There is no
version boundary and no publish step: an edit here is live in `PortalsAPI-B`, `CL-Portal`,
`CLAPI-C`, `IntegrationLayer`, `Portals-FE-Portal`, `Core`, `CL-Core`, `Portals-Core` and the
rest **on their next build**.

There are also **no test projects in this repo**. Nothing here is verified in isolation;
verification comes from the consumers compiling and running. Treat every change as a
cross-workspace change: check who references the project you are touching before editing,
and build at least one consumer afterwards.

`HCore-Identity` and `HCore-Web` are the highest-traffic projects — nearly every service
references them, directly or transitively.

## 2. It hosts the shared codegen toolchain

`OpenAPI/` is not about HCore's own API — it is the tooling **every API repo shells out to**
when regenerating clients and controllers:

| Path | What it is |
|---|---|
| `OpenAPI/openapi-generator-cli.jar` | the generator invoked by the `generateSmintIo*` scripts |
| `OpenAPI/NSwag` | vendored NSwag binaries, with `Net60` / `Net70` / `Net80` runners and `nswag.cmd` |
| `OpenAPI/Templates/Server.NETCore` | server controller/model templates |
| `OpenAPI/Templates/TypeScript` | TypeScript client templates |

**Editing a template here changes generated output in every API repo.** It will not show up
until each repo re-runs its generate script, and those runs are currently blocked by stale
patches in `PortalsAPI-B`, `CLAPI-C` and `CL-Portal` — see the workspace map.

## Internal layering

Dependencies inside the repo run bottom-up. Leaf projects with no internal references:
`HCore-Amqp`, `HCore-Cache`, `HCore-Directory`, `HCore-Metadata`, `HCore-Pusher`,
`HCore-Rest`, `HCore-Scheduling`, `HCore-Segment`, `HCore-Translations`.

| Project | References |
|---|---|
| `HCore-Web` | `-Amqp`, `-Segment`, `-Translations` |
| `HCore-Database` | `-Web` |
| `HCore-Storage` | `-Web` |
| `HCore-Tenants` | `-Database`, `-Directory`, `-Storage`, `-Web` |
| `HCore-Templating` | `-Tenants` |
| `HCore-Emailing` | `-Amqp`, `-Tenants` |
| `HCore-Identity` | `-Amqp`, `-Cache`, `-Database`, `-Directory`, `-Emailing`, `-Rest`, `-Templating`, `-Tenants`, `-Web` |
| `HCore-PagesUI-Classes` | `-Identity` |
| `HCore-PagesUI-Views` | `-Identity`, `-PagesUI-Classes` |
| `HCore-Identity-PagesUI-Classes` | `-Identity`, `-Segment` |
| `HCore-Identity-PagesUI-Views` | `-Identity`, `-Identity-PagesUI-Classes` |

`HCore-Identity` sits at the top of the graph and is the largest project (67 `.cs`). A change
there is the widest-reaching change you can make in this workspace.

## Conventions worth knowing

- **`-Classes` vs `-Views` split.** The `PagesUI` pairs separate C# from Razor: `-Classes`
  holds the code (`HCore-Identity-PagesUI-Classes`, 19 `.cs`, no `.cshtml`), `-Views` holds
  the markup (`HCore-Identity-PagesUI-Views`, 23 `.cshtml`, no `.cs`). Put code in one and
  markup in the other; don't mix.
- **`HCore-Directory` is an empty project** — a `.csproj` with no source files — yet
  `HCore-Tenants` and `HCore-Identity` both reference it. Leave it in place; removing the
  reference is a consumer-visible change, and the empty state may be deliberate.
- **`ConcatenateTokenFilter/`** holds prebuilt Elasticsearch plugin zips (6.4.0 through
  8.19.3), matching the separate `elasticsearch-concatenate-token-filter` repo, which is
  **out of scope** for this configuration. Binary artifacts — don't try to edit them.
- HCore is written as general-purpose infrastructure, not Smint.io product code. Keep
  Smint.io-specific logic out of it; it belongs in `Core`, `Portals-Core` or `CL-Core`.
