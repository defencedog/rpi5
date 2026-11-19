# Pinta 3.1.2 GTK 4.8 / libadwaita 1.2 backport for Raspberry Pi 5

This directory contains an unofficial arm64 build of **Pinta 3.1.2** for
**Raspberry Pi OS Bookworm (Debian 12)**, backported from upstream's
GTK 4.10+ / libadwaita 1.6+ requirements to the GTK 4.8.3 /
libadwaita 1.2.2 stack shipped with Bookworm.

The resulting `.deb` is self-contained: it bundles the .NET 8 runtime,
so users do not need a separate dotnet installation.

## Package

- Name: `pinta-3.1.2`
- Version: `3.1.2+rpi5backport2`
- Architecture: `arm64`
- Installs to: `/usr/lib/pinta-3.1.2`, launcher `/usr/bin/pinta-3.1.2`
- Desktop entry: `/usr/share/applications/com.github.PintaProject.Pinta.desktop`
- Icon: `/usr/share/icons/hicolor/.../apps/com.github.PintaProject.Pinta.*`

The package coexists with a manual Pinta 2.1.x install under
`/usr/local/lib/pinta`: the old `pinta` launcher is not touched.

## Built package

- File: `pinta-3.1.2_3.1.2+rpi5backport2_arm64.deb`
- Size: 27 MB (108 MB installed)
- SHA-256: `0b20d6daf8817e1937077f1408a10aaa20b5bb4998c69512f7aa285451698fe8`

Install:

```sh
sudo apt install ./pinta-3.1.2_3.1.2+rpi5backport2_arm64.deb
pinta-3.1.2
```

## Build

On an arm64 Debian 12 / Raspberry Pi OS Bookworm host with the .NET 8 SDK:

```sh
git clone https://github.com/PintaProject/Pinta.git
cd Pinta
git checkout 3.1.2
patch -p1 < /path/to/pinta-3.1.2-rpi5/patches/pinta-3.1.2-gtk48-backport.patch

dotnet publish Pinta/Pinta.csproj -c Release -r linux-arm64 \
    --self-contained true -p:PublishSingleFile=false -o publish

/path/to/pinta-3.1.2-rpi5/build-deb.sh publish
```

The build script creates
`pinta-3.1.2_3.1.2+rpi5backport2_arm64.deb` and prints its SHA-256.

`dotnet publish` can also be run on x86_64 with the .NET 8 SDK; it
cross-publishes the arm64 runtime pack. The resulting apphost was tested
on a Raspberry Pi 5 running Debian 12 arm64.

## Install

```sh
sudo apt install ./pinta-3.1.2_3.1.2+rpi5backport2_arm64.deb
pinta-3.1.2
```

Or launch **Pinta 3.1.2** from the application menu.

## Backport changes

Source: upstream tag `3.1.2` (commit `b3df1e5`).

| Upstream API (GTK 4.10+) | Bookworm replacement (GTK 4.8) |
| --- | --- |
| `Adw.ToolbarView` | vertical `Gtk.Box` |
| `Gtk.FileDialog` | `Gtk.FileChooserNative` |
| `Gtk.FontDialogButton` | `Gtk.FontButton` |
| `Gtk.UriLauncher` | `gtk_show_uri()` |
| `Gtk.Snapshot.AppendScaledTexture` | `Gtk.Snapshot.AppendTexture` |
| `Gdk.MemoryTextureBuilder` | managed Cairo ARGB32 → `GdkPixbuf` + `Gdk.Texture.NewForPixbuf` |
| `gtk_css_provider_load_from_bytes` | `gtk_css_provider_load_from_data` |

Also fixed:

- a `GSList` ownership/double-free bug in `gdk_pixbuf_get_formats()`
  and `gdk_file_list_get_files()`;
- the window/taskbar icon by setting `Gtk.Window.SetDefaultIconName`
  and `GLib.SetPrgname("pinta")` and installing the app icon;
- the missing Adjustments/Effects header icons by adding the
  application-local `icons` directory to GTK's icon search path (the
  self-contained layout lives under `/usr/lib/pinta-3.1.2`) and using
  `currentColor` in the effects SVG so GTK 4.8 can recolor it.

The full diff is in `patches/pinta-3.1.2-gtk48-backport.patch` and is
also installed in the package under `/usr/share/doc/pinta-3.1.2/`.

## Known limitations

- The canvas texture is converted from Cairo ARGB32 to a `GdkPixbuf`
  in managed code because Debian builds gdk-pixbuf without Cairo
  (`gdk_pixbuf_get_from_surface` is unavailable). Very large canvases
  may redraw more slowly than upstream on newer GTK.
- File dialogs use `Gtk.FileChooserNative`; `xdg-desktop-portal-gtk`
  is recommended for best desktop integration.
- The font chooser uses `Gtk.FontButton`.
- Links are opened with `gtk_show_uri()`.
- GTK 4.10+ features such as scaled-texture filter hints are not
  available.

## Tested on

- Raspberry Pi 5, Debian 12 arm64
- GTK 4.8.3, libadwaita 1.2.2
- .NET 8 SDK 8.0.428 (build), .NET 8.0.14 runtime (bundled publish)
- XFCE/X11 session over xrdp

This is an unofficial community backport. Pinta is MIT-licensed; see
`packaging/copyright`.
