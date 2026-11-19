#!/bin/sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PUBLISH_DIR=${1:-/tmp/pinta-publish-arm64}
OUT=${2:-"$SCRIPT_DIR/pinta-3.1.2_3.1.2+rpi5backport2_arm64.deb"}

if [ ! -x "$PUBLISH_DIR/Pinta" ]; then
    echo "error: $PUBLISH_DIR/Pinta not found or not executable" >&2
    exit 1
fi

ROOT=$(mktemp -d)
chmod 0755 "$ROOT"
trap 'rm -rf "$ROOT"' EXIT

LIBDIR="$ROOT/usr/lib/pinta-3.1.2"
DOCDIR="$ROOT/usr/share/doc/pinta-3.1.2"

install -d "$ROOT/DEBIAN" "$LIBDIR" "$ROOT/usr/bin" \
    "$ROOT/usr/share/applications" "$ROOT/usr/share/metainfo" \
    "$ROOT/usr/share/man/man1" "$DOCDIR" \
    "$ROOT/usr/share/icons/hicolor/scalable/apps"

cp -a "$PUBLISH_DIR/." "$LIBDIR/"

# Normalize modes: managed files do not need +x; native loader keeps it.
find "$LIBDIR" -type d -exec chmod 0755 {} +
find "$LIBDIR" -type f \( -name '*.dll' -o -name '*.json' -o -name '*.pdb' \
    -o -name '*.xml' -o -name '*.txt' \) -exec chmod 0644 {} +
find "$LIBDIR" -type f -name '*.so*' -exec chmod 0644 {} +
chmod 0755 "$LIBDIR/Pinta"
if [ -f "$LIBDIR/createdump" ]; then
    chmod 0755 "$LIBDIR/createdump"
fi

ln -s /usr/lib/pinta-3.1.2/Pinta "$ROOT/usr/bin/pinta-3.1.2"

for size in 16x16 22x22 24x24 32x32 96x96; do
    src="$PUBLISH_DIR/icons/hicolor/$size/apps/com.github.PintaProject.Pinta.png"
    [ -f "$src" ] || continue
    install -D -m 0644 "$src" \
        "$ROOT/usr/share/icons/hicolor/$size/apps/com.github.PintaProject.Pinta.png"
done

svg="$PUBLISH_DIR/icons/hicolor/scalable/apps/com.github.PintaProject.Pinta.svg"
if [ -f "$svg" ]; then
    install -D -m 0644 "$svg" \
        "$ROOT/usr/share/icons/hicolor/scalable/apps/com.github.PintaProject.Pinta.svg"
fi

install -m 0644 "$SCRIPT_DIR/packaging/com.github.PintaProject.Pinta.desktop" \
    "$ROOT/usr/share/applications/com.github.PintaProject.Pinta.desktop"
install -m 0644 "$SCRIPT_DIR/packaging/com.github.PintaProject.Pinta.metainfo.xml" \
    "$ROOT/usr/share/metainfo/com.github.PintaProject.Pinta.metainfo.xml"
install -m 0644 "$SCRIPT_DIR/packaging/pinta-3.1.2.1" \
    "$ROOT/usr/share/man/man1/pinta-3.1.2.1"
gzip -9 -n "$ROOT/usr/share/man/man1/pinta-3.1.2.1"

install -m 0644 "$SCRIPT_DIR/packaging/copyright" "$DOCDIR/copyright"
install -m 0644 "$SCRIPT_DIR/packaging/README.Debian" "$DOCDIR/README.Debian"
gzip -9 -n -c "$SCRIPT_DIR/packaging/changelog.Debian" > "$DOCDIR/changelog.Debian.gz"

if [ -f "$SCRIPT_DIR/patches/pinta-3.1.2-gtk48-backport.patch" ]; then
    gzip -9 -n -c "$SCRIPT_DIR/patches/pinta-3.1.2-gtk48-backport.patch" \
        > "$DOCDIR/pinta-3.1.2-gtk48-backport.patch.gz"
fi

SIZE=$(du -sk "$ROOT/usr" | cut -f1)
sed "s/@SIZE@/$SIZE/" "$SCRIPT_DIR/packaging/control" > "$ROOT/DEBIAN/control"
install -m 0755 "$SCRIPT_DIR/packaging/postinst" "$ROOT/DEBIAN/postinst"
install -m 0755 "$SCRIPT_DIR/packaging/postrm" "$ROOT/DEBIAN/postrm"

( cd "$ROOT" && find usr -type f -exec md5sum {} + | sort -k2 > DEBIAN/md5sums )

dpkg-deb --root-owner-group --build "$ROOT" "$OUT"
sha256sum "$OUT"
