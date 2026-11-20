# Patched source files (overlay)

These are **only** the files that differ from upstream Pinta **tag `3.1.2`**
(commit `b3df1e5`). The full source tree is deliberately *not* kept in this
directory.

The same changes are also available as a unified diff in
`../patches/pinta-3.1.2-gtk48-backport.patch`.

## Reproduce the build

```sh
git clone https://github.com/PintaProject/Pinta.git
cd Pinta
git checkout 3.1.2

# overlay the patched files
cp -a /path/to/pinta-3.1.2-rpi5/patched/Pinta* .

# remove the file the backport deletes
rm Pinta.Core/Extensions/GskExtensions.cs

dotnet publish Pinta/Pinta.csproj -c Release -r linux-arm64 \
    --self-contained true -p:PublishSingleFile=false -o publish

/path/to/pinta-3.1.2-rpi5/build-deb.sh publish
```

## Patched files

| File | What changed |
| --- | --- |
| `Pinta.Core/Extensions/GdkExtensions.cs` | `gdk_file_list_get_files` `GSList` ownership fix |
| `Pinta.Core/Extensions/GdkPixbufExtensions.cs` | `GSList` ownership fix; Cairo ARGB32 → `GdkPixbuf` (Debian lacks `gdk_pixbuf_get_from_surface`) |
| `Pinta.Core/Extensions/OtherExtensions.cs` | `Gtk.UriLauncher` → `gtk_show_uri()` |
| `Pinta.Core/Actions/EditActions.cs` | "Open Palette": `Gtk.FileDialog` → `FileChooserNative` |
| `Pinta.Core/Actions/LayerActions.cs` | "Open Image": `Gtk.FileDialog` → `FileChooserNative` |
| `Pinta/Actions/File/OpenDocumentAction.cs` | `Gtk.FileDialog` → `FileChooserNative` |
| `Pinta.Resources/ResourceManager.cs` | `gtk_css_provider_load_from_bytes` → `load_from_data` |
| `Pinta.Resources/icons/hicolor/scalable/actions/effects-default-symbolic.svg` | `currentColor` so GTK 4.8 can recolor it |
| `Pinta/Main.cs` | `GLib.SetPrgname("pinta")`; add app-local `icons/` to the icon theme search path |
| `Pinta/MainWindow.cs` | keep strong references to the `Gio.Menu` objects (menu lifetime) |
| `Pinta/WindowShell.cs` | `Adw.ToolbarView` → `Gtk.Box` |
| `Pinta.Tools/Tools/TextTool.cs` | `Gtk.FontDialogButton` → `Gtk.FontButton` |
| `Pinta.Gui.Widgets/Widgets/Canvas/PintaCanvas.cs` | **GTK 4.8 fix:** selection outline + pixel/axonometric grids drawn with `Gtk.Snapshot.AppendCairo` (was `Gsk.PathBuilder` / `Gsk.Path` / `Gsk.Stroke`) |
| `Pinta.Tools/Handles/MoveHandle.cs` | **GTK 4.8 fix:** selection handle drawn with `Gtk.Snapshot.AppendCairo` (was `Gsk.PathBuilder`) |

## Deleted

- `Pinta.Core/Extensions/GskExtensions.cs` — the manual
  `gsk_path_builder_add_cairo_path` binding. No longer needed once the
  selection/handles/grids use `Gtk.Snapshot.AppendCairo`, which GTK 4.8
  supports.
