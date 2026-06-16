# Continuum

Continuum is an early-stage Jellyfin plugin that turns manually curated, ordered lists of movies and TV episodes into rolling per-user playlists.

It is intentionally deterministic: you define the order, Continuum resolves each entry against the local Jellyfin library, then creates or updates a private playlist for each user that contains only their next eligible items.

## Current Status

This repository is a starter plugin scaffold. It builds the plugin architecture, configuration, scheduled task, list models, state store, and core services needed to extend Continuum safely.

Supported item types today:

- Movies
- Episodes

Supported matching today:

- Explicit Jellyfin item id
- TMDb movie id
- IMDb id
- TVDb episode id
- TMDb episode id
- TVDb series id plus season/episode
- TMDb series id plus season/episode
- Title and year or episode title fallback when unambiguous

## How It Works

1. Continuum fetches a live `Playlist-Data/playlist-manifest.json` file from raw GitHub.
2. The manifest tells Continuum which live `*.jsonc` playlist files to fetch from the same archive.
3. Each manual entry is resolved against existing Jellyfin library metadata.
4. Resolved entries keep the original manual order.
5. For every eligible Jellyfin user, Continuum filters out fully watched items.
6. The next `PlaylistSize` eligible items are written to that user's Continuum playlist.

Safety behavior in this starter:

- Missing items are skipped instead of guessed.
- Ambiguous matches are skipped and logged.
- Unrelated user playlists are never touched.
- If a refresh suddenly resolves zero eligible items while an existing playlist previously had content, Continuum skips the destructive update and logs a warning.

## Configuration

Continuum exposes a basic plugin configuration page with:

- `Enabled`
- `RefreshIntervalMinutes`
- `PlaylistSize`
- `PlaylistSuffix`
- `EnabledLists`

Additional stored config fields already exist for future work:

- `CreatePlaylistsForDisabledUsers`
- `IncludePartiallyWatched`
- `IncludeUnwatched`
- `PlaylistImagePaths`

Sanitization rules:

- `RefreshIntervalMinutes` minimum: `5`
- `PlaylistSize` range: `1..150`
- Default refresh interval: `60`
- Default playlist size: `100`
- Per-list enablement defaults to `off` until each server admin checks the list on the Continuum admin page
- If both watch-state include flags are false, Continuum treats both as enabled and logs a warning

## Manual List Format

Continuum v1 uses JSONC so list files can include comments. Files live in this repository under:

```text
Playlist-Data/*.jsonc
Playlist-Data/playlist-manifest.json
```

Example:

```json
{
  "name": "Chicago Universe",
  "slug": "chicago-universe",
  "description": "Manual chronological watch order for the Chicago franchise.",
  "items": [
    {
      "type": "episode",
      "title": "Chicago Fire S01E01",
      "providers": {
        "tvdbSeriesId": "258541",
        "tmdbSeriesId": "44006"
      },
      "seasonNumber": 1,
      "episodeNumber": 1
    },
    {
      "type": "movie",
      "title": "Example Movie",
      "providers": {
        "tmdbMovieId": "123456",
        "imdbId": "tt1234567"
      },
      "year": 2020
    }
  ]
}
```

At runtime, the plugin fetches the raw `main` branch versions of those files directly from GitHub, using `Playlist-Data/playlist-manifest.json` to discover which playlist files are live. Item order comes directly from array position, so you do not need to set an `order` property on each entry. Archive files do not control whether a list is active on a given server; each admin enables lists locally from the Continuum admin page, and new lists start disabled by default.

The community archive is indexed by [`Playlist-Data/playlist-manifest.json`](/Volumes/Vault/Projects/CursedCode/Continuum/Playlist-Data/playlist-manifest.json) and currently includes [`Playlist-Data/chicago-universe.jsonc`](/Volumes/Vault/Projects/CursedCode/Continuum/Playlist-Data/chicago-universe.jsonc), [`Playlist-Data/star-wars-test.jsonc`](/Volumes/Vault/Projects/CursedCode/Continuum/Playlist-Data/star-wars-test.jsonc), and [`Playlist-Data/marvel-cinematic-universe.jsonc`](/Volumes/Vault/Projects/CursedCode/Continuum/Playlist-Data/marvel-cinematic-universe.jsonc).

To contribute a community playlist archive entry, add or update a JSONC file in `Playlist-Data/`, update `Playlist-Data/playlist-manifest.json`, merge to `main`, and Continuum will pick it up on the next refresh without a plugin release.

## Build

```bash
dotnet restore
dotnet build Continuum.slnx
dotnet test Continuum.slnx
```

The project targets `net9.0` and references Jellyfin `10.11.3` packages to match the current official plugin template guidance.

## Releasing

Continuum releases are built by GitHub Actions.

### Required secret

This repo requires:

- `MANIFEST_REPO_TOKEN`

The token must be able to call `repository_dispatch` on:

`CursedCodeStudios/Jellyfin-Plugins`

### Create a release

```bash
git tag v0.1.5.0
git push origin v0.1.5.0
```

The release workflow will:

1. build the plugin;
2. create `Continuum_0.1.5.0.zip`;
3. calculate SHA256;
4. create/update the GitHub Release;
5. trigger `CursedCodeStudios/Jellyfin-Plugins`;
6. update the Jellyfin plugin manifest through that repo’s workflow.

### Jellyfin repository URL

Add this to Jellyfin:

```text
https://raw.githubusercontent.com/CursedCodeStudios/Jellyfin-Plugins/main/manifest.json
```

In Jellyfin:

`Dashboard → Plugins → Repositories → Add`

Name:

```text
CursedCodeStudios Jellyfin Plugins
```

URL:

```text
https://raw.githubusercontent.com/CursedCodeStudios/Jellyfin-Plugins/main/manifest.json
```

## Manual Install

1. Build the project.
2. Copy the contents of `Continuum/bin/<Configuration>/net9.0/` into your Jellyfin plugin folder, or package it according to your server setup.
3. Restart Jellyfin.
4. Open the Continuum plugin settings page in the admin dashboard.
5. Run the scheduled task `Refresh Continuum Playlists` or wait for the interval trigger.

## Known Limitations

- JSONC only; YAML is not implemented yet.
- The live playlist archive depends on GitHub availability and successful outbound HTTP access from the Jellyfin server.
- The admin controller endpoints are intentionally omitted in this starter because the current plugin package surface for controller registration should be verified against a live Jellyfin host.
- Refresh interval changes are not dynamically pushed into Jellyfin's task scheduler; restarting Jellyfin or manually adjusting the task may be required.
- The episode fallback matcher is intentionally conservative and skips ambiguous results.
- Playlist artwork mapping is stored in config but not yet applied.

## Future Ideas

- YAML list support
- A browser-based list editor
- Refresh on playback stopped
- Refresh after library scan
- Per-list playlist size overrides
- Import from Trakt or TMDb lists
