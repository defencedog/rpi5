//
// GdkExtensions.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using GdkPixbuf;

namespace Pinta.Core;

public static partial class GdkPixbufExtensions
{
	private const string PixbufLibraryName = "GdkPixbuf";

	static GdkPixbufExtensions ()
	{
		NativeImportResolver.RegisterLibrary (PixbufLibraryName,
			windowsLibraryName: "libgdk_pixbuf-2.0-0.dll",
			linuxLibraryName: "libgdk_pixbuf-2.0.so.0",
			osxLibraryName: "libgdk_pixbuf-2.0.0.dylib"
		);
	}

	// TODO-GTK4 (bindings, unsubmitted) - needs support for primitive value arrays
	public static byte[] SaveToBuffer (this Pixbuf pixbuf, string type)
	{
		SaveToBufferv (pixbuf.Handle.DangerousGetHandle (), out IntPtr buffer, out uint buffer_size, type, IntPtr.Zero, IntPtr.Zero, out var error);
		if (!error.IsInvalid)
			throw new GLib.GException (error);

		var result = new byte[buffer_size];
		Marshal.Copy (buffer, result, 0, (int) buffer_size);
		GLib.Functions.Free (buffer);
		return result;
	}

	// Higher-level wrapper around GdkPixbuf.Pixbuf.GetFormats(), which returns only a GLib.SList.
	// TODO-GTK4 (bindings) - record methods are not generated (https://github.com/gircore/gir.core/issues/743)
	public static PixbufFormat[] GetFormats ()
	{
		var list = new GLib.Internal.SListUnownedHandle (GetFormatsNative ());
		uint n = GLib.Internal.SList.Length (list);
		var result = new PixbufFormat[n];
		for (uint i = 0; i < n; ++i) {
			var format = new GdkPixbuf.Internal.PixbufFormatUnownedHandle (GLib.Internal.SList.NthData (list, i));
			result[i] = new PixbufFormat (GdkPixbuf.Internal.PixbufFormat.Copy (format));
		}

		return result;
	}

	// GTK 4.8 / Debian compatibility: gdk_pixbuf_get_from_surface() is unavailable because
	// Debian builds gdk-pixbuf without Cairo support. Convert the premultiplied Cairo
	// ARGB32 surface (BGRA on little-endian) into straight RGBA for GdkPixbuf instead.
	public static GdkPixbuf.Pixbuf GetPixbufFromSurface (Cairo.ImageSurface surface)
	{
		int width = surface.Width;
		int height = surface.Height;
		int src_stride = surface.Stride;
		int dst_stride = width * 4;
		var src = surface.GetData ();
		var dst = new byte[dst_stride * height];

		for (int y = 0; y < height; ++y) {
			var src_row = src.Slice (y * src_stride, width * 4);
			int dst_row = y * dst_stride;

			for (int x = 0; x < width; ++x) {
				int i = x * 4;
				byte b = src_row[i];
				byte g = src_row[i + 1];
				byte r = src_row[i + 2];
				byte a = src_row[i + 3];

				if (a != 0 && a != 255) {
					r = (byte) Math.Min (255, r * 255 / a);
					g = (byte) Math.Min (255, g * 255 / a);
					b = (byte) Math.Min (255, b * 255 / a);
				}

				dst[dst_row + i] = r;
				dst[dst_row + i + 1] = g;
				dst[dst_row + i + 2] = b;
				dst[dst_row + i + 3] = a;
			}
		}

		using var bytes = GLib.Bytes.New (dst);
		return GdkPixbuf.Pixbuf.NewFromBytes (bytes, GdkPixbuf.Colorspace.Rgb, true, 8, width, height, dst_stride);
	}

	[DllImport (PixbufLibraryName, EntryPoint = "gdk_pixbuf_get_formats")]
	private static extern IntPtr GetFormatsNative ();

	[DllImport (PixbufLibraryName, EntryPoint = "gdk_pixbuf_save_to_bufferv")]
	private static extern bool SaveToBufferv (IntPtr pixbuf, out IntPtr buffer, out uint buffer_size, [MarshalAs (UnmanagedType.LPUTF8Str)] string type, IntPtr option_keys, IntPtr option_values, out GLib.Internal.ErrorOwnedHandle error);
}
