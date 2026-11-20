//
// PintaCanvas.cs
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
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class PintaCanvas : Gtk.Picture
{
	private readonly CanvasRenderer cr;
	private readonly Document document;
	private readonly ICanvasGridService canvas_grid;

	private uint queued_update_id = 0;

	private static readonly Gdk.Texture transparent_pattern_texture = CreateTransparentPatternTexture ();

	private Cairo.ImageSurface? canvas_surface;
	private Gdk.Texture? canvas_texture;
	private RectangleI? modified_area;

	private readonly uint selection_animation_timer_id;
	private float selection_animation_dash_offset;

	private readonly ToolManager tools;

	public PintaCanvas (
		ToolManager tools,
		Document document,
		ICanvasGridService canvasGrid)
	{
		this.tools = tools;
		canvas_grid = canvasGrid;
		this.document = document;

		cr = new (
			PintaCore.LivePreview,
			PintaCore.Workspace,
			enableLivePreview: true,
			enableBackgroundPattern: false);

		document.Workspace.ViewSizeChanged += OnViewSizeChanged;
		document.Workspace.CanvasInvalidated += OnCanvasInvalidated;
		document.SelectionChanged += (_, _) => QueueUpdate ();

		// Timer for selection outline animation
		selection_animation_timer_id = GLib.Functions.TimeoutAdd (GLib.Constants.PRIORITY_DEFAULT, 80, SelectionAnimationTick);

		// If there is additional space available, keep the image centered and prevent stretching.
		Hexpand = false;
		Halign = Gtk.Align.Center;
		Vexpand = false;
		Valign = Gtk.Align.Center;
	}

	/// <summary>
	/// Queue an update to the canvas texture.
	/// There can be multiple consecutive Invalidate() calls before a UI update, e.g.
	/// in the text tool, or undoing multiple history items.
	/// </summary>
	private void OnCanvasInvalidated (object? o, CanvasInvalidatedEventArgs e)
	{
		RectangleI rect = e.EntireSurface
			? new RectangleI (PointI.Zero, document.ImageSize)
			: e.Rectangle;

		// If an update is already queued, just extend the region to update.
		if (modified_area.HasValue) {
			modified_area = modified_area.Value.Union (rect);
			return;
		}

		modified_area = rect;
		QueueUpdate ();
	}

	/// <summary>
	/// Queue an update to the widget's contents on the next UI update, e.g. after changes
	/// to the canvas contents or the selection.
	/// This is useful to avoid redundant work if there are multiple events that trigger
	/// changes to the document.
	/// </summary>
	private void QueueUpdate ()
	{
		if (queued_update_id > 0)
			return;

		queued_update_id = GLib.Functions.IdleAdd (GLib.Constants.PRIORITY_DEFAULT, () => {
			UpdateCanvas ();
			queued_update_id = 0;
			return false;
		});
	}

	/// <summary>
	/// Update the canvas after changes to the image.
	/// </summary>
	private void UpdateCanvas ()
	{
		Graphene.Rect canvasViewBounds = Graphene.Rect.Alloc ();
		Size viewSize = document.Workspace.ViewSize;
		canvasViewBounds.Init (0.0f, 0.0f, (float) viewSize.Width, (float) viewSize.Height);

		Gtk.Snapshot snapshot = Gtk.Snapshot.New ();

		DrawTransparentBackground (snapshot, canvasViewBounds);
		DrawCanvasTexture (snapshot, modified_area, canvasViewBounds);
		DrawSelection (snapshot, canvasViewBounds);
		DrawHandles (snapshot, canvasViewBounds);
		DrawCanvasGrid (snapshot, canvasViewBounds);
		DrawCanvasAxonometricGrid (snapshot, canvasViewBounds);

		// In the future, this would be cleaner to implement as a custom widget once gir.core supports virtual methods
		// (in particular, zooming might be easier when we have control over the size allocation)
		// For now, we just use a Gtk.Picture widget with a custom Gdk.Paintable for its contents.
		Gdk.Paintable? paintable = snapshot.ToPaintable (size: null);
		if (paintable is not null)
			Paintable = paintable;
		else
			System.Console.WriteLine ("Failed to render snapshot for canvas");

		modified_area = null;
		QueueDraw ();
	}

	private static Gdk.Texture CreateTextureFromSurface (
		Cairo.ImageSurface surface,
		Gdk.Texture? updateTexture = null,
		Cairo.Region? updateRegion = null)
	{
		// GTK 4.8 compatibility: Gdk.MemoryTextureBuilder requires GTK 4.10+.
		// Use gdk_pixbuf_get_from_surface + gdk_texture_new_for_pixbuf instead.
		_ = updateTexture;
		_ = updateRegion;
		using GdkPixbuf.Pixbuf pixbuf = GdkPixbufExtensions.GetPixbufFromSurface (surface);
		return Gdk.Texture.NewForPixbuf (pixbuf);
	}

	private static Gdk.Texture CreateTransparentPatternTexture () =>
		CreateTextureFromSurface (CairoExtensions.CreateTransparentBackgroundSurface (size: 16));

	/// <summary>
	/// Draw the transparent checkboard background by tiling a small pattern.
	/// </summary>
	private void DrawTransparentBackground (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		snapshot.PushRepeat (canvasViewBounds, childBounds: null);

		Graphene.Rect patternBounds = Graphene.Rect.Alloc ();
		patternBounds.Init (0, 0, transparent_pattern_texture.Width, transparent_pattern_texture.Height);
		snapshot.AppendTexture (transparent_pattern_texture, patternBounds);

		snapshot.Pop ();
	}

	private void DrawCanvasTexture (Gtk.Snapshot snapshot, RectangleI? modifiedArea, Graphene.Rect canvasViewBounds)
	{
		// Update the texture if the canvas contents have changed.
		if (modifiedArea.HasValue) {
			// Compute the flattened image for the modified region.
			if (canvas_surface is null ||
			    canvas_surface.Width != document.ImageSize.Width ||
			    canvas_surface.Height != document.ImageSize.Height) {

				canvas_surface?.Dispose ();
				canvas_surface = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, document.ImageSize.Width, document.ImageSize.Height);

				canvas_texture?.Dispose ();
				canvas_texture = null;
			}

			// Note we are always rendering without scaling, since the scaling is applied when drawing the texture later.
			// TODO - in the future we could experiment with creating a separate texture per layer and using gtk_snapshot_push_blend() to blend on the GPU
			cr.Initialize (document.ImageSize, document.ImageSize);

			List<Layer> layers = document.Layers.GetLayersToPaint ().ToList ();
			cr.Render (layers, canvas_surface, offset: PointI.Zero, clipRect: modifiedArea);

			Gdk.Texture? updateTexture = canvas_texture;
			Cairo.Region? updateRegion = (updateTexture is not null)
				? CairoExtensions.CreateRegion (modifiedArea.Value)
				: null;
			canvas_texture = CreateTextureFromSurface (canvas_surface, updateTexture, updateRegion);
		}

		if (canvas_texture is null)
			throw new InvalidOperationException ("Canvas was never invalidated!");

		// GTK 4.8 compatibility: gtk_snapshot_append_scaled_texture() requires GTK 4.10+.
		// gtk_snapshot_append_texture() also scales the texture to fit the given bounds.
		snapshot.AppendTexture (canvas_texture, canvasViewBounds);
	}

	private void DrawSelection (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		if (!document.Selection.Visible)
			return;

		bool fillSelection = tools.CurrentTool?.IsSelectionTool ?? false;

		snapshot.PushClip (canvasViewBounds);

		// GTK 4.8 compatibility: Gsk.PathBuilder, Gsk.Path, Gsk.Stroke,
		// Snapshot.AppendFill and Snapshot.AppendStroke require GTK 4.14+.
		// Draw the selection outline with a Cairo node instead.
		//
		// Scale the selection path up to the view size.
		// Note the outline width (below) remains at a constant size.
		float scale = (float) document.Workspace.Scale;
		using Cairo.Context cr = snapshot.AppendCairo (canvasViewBounds);
		cr.Scale (scale, scale);

		cr.NewPath ();
		cr.AppendPath (document.Selection.SelectionPath);

		if (fillSelection) {
			Gdk.RGBA fillColor = new () { Red = 0.7f, Green = 0.8f, Blue = 0.9f, Alpha = 0.2f };
			cr.FillRule = Cairo.FillRule.EvenOdd;
			cr.SetSourceRgba (fillColor.Red, fillColor.Green, fillColor.Blue, fillColor.Alpha);
			cr.FillPreserve ();
		}

		// Draw a white line first so it shows up on dark backgrounds
		cr.SetSourceRgba (1, 1, 1, 1);
		cr.LineWidth = 1.0 / scale;
		cr.StrokePreserve ();

		// Draw a black dashed line over the white line
		cr.SetSourceRgba (0, 0, 0, 1);
		cr.LineWidth = 1.0 / scale;
		cr.SetDash (new double[] { 2.0 / scale, 4.0 / scale }, selection_animation_dash_offset / scale);
		cr.Stroke ();

		snapshot.Pop ();
	}

	private void DrawHandles (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		BaseTool? tool = tools.CurrentTool;
		if (tool is null)
			return;

		snapshot.PushClip (canvasViewBounds);

		foreach (IToolHandle control in tool.Handles.Where (c => c.Active)) {
			control.Draw (snapshot);
		}

		snapshot.Pop ();
	}

	private void DrawCanvasGrid (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		if (!ShouldShowCanvasGrid ())
			return;

		snapshot.PushClip (canvasViewBounds);

		// GTK 4.8 compatibility: Gsk.Path and Gsk.Stroke require GTK 4.14+.
		// Draw the grid with a Cairo node instead.
		float scale = (float) document.Workspace.Scale;
		using Cairo.Context cr = snapshot.AppendCairo (canvasViewBounds);
		cr.Scale (scale, scale);

		int cellHeight = canvas_grid.CellHeight;
		int cellWidth = canvas_grid.CellWidth;
		int imageHeight = document.ImageSize.Height;
		int imageWidth = document.ImageSize.Width;

		// Draw as a dotted line (every other pixel) to have a more subtle appearance.
		cr.SetSourceRgba (0, 0, 0, 1);
		cr.LineWidth = 1.0 / scale;
		cr.SetDash (new double[] { 1.0 / scale, 1.0 / scale }, 0);

		// Add horizontal lines.
		for (int y = 0; y < imageHeight; y += cellHeight) {
			cr.MoveTo (0, y);
			cr.LineTo (imageWidth, y);
		}

		// Add vertical lines.
		for (int x = 0; x < imageWidth; x += cellWidth) {
			cr.MoveTo (x, 0);
			cr.LineTo (x, imageHeight);
		}

		cr.Stroke ();

		snapshot.Pop ();
	}

	/// <summary>
	/// The grid should be drawn if it is enabled and we're zoomed in far enough.
	/// </summary>
	private bool ShouldShowCanvasGrid ()
	{
		if (!canvas_grid.ShowGrid)
			return false;

		const int MIN_GRID_LINE_DISTANCE = 5;

		int cellHeight = canvas_grid.CellHeight;
		int cellWidth = canvas_grid.CellWidth;

		int minCanvasDistance = Math.Min (cellHeight, cellWidth);
		int minViewDistance = (int) Math.Ceiling (minCanvasDistance * document.Workspace.Scale);

		return minViewDistance >= MIN_GRID_LINE_DISTANCE;
	}

	private void DrawCanvasAxonometricGrid (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		if (!ShouldShowCanvasAxonometricGrid ())
			return;

		snapshot.PushClip (canvasViewBounds);

		// GTK 4.8 compatibility: Gsk.Path and Gsk.Stroke require GTK 4.14+.
		// Draw the grid with a Cairo node instead.
		float scale = (float) document.Workspace.Scale;
		using Cairo.Context cr = snapshot.AppendCairo (canvasViewBounds);
		cr.Scale (scale, scale);

		int axonometricWidth = canvas_grid.AxonometricWidth;
		int imageHeight = document.ImageSize.Height;
		int imageWidth = document.ImageSize.Width;

		// Draw as a dotted line (every other pixel) to have a more subtle appearance.
		cr.SetSourceRgba (0, 0, 0, 1);
		cr.LineWidth = 1.0 / scale;
		cr.SetDash (new double[] { 1.0 / scale, 1.0 / scale }, 0);

		// Adds ascending diagonal lines.
		// '2 * axonometricWidth' ensures the vertical lines will align correctly.
		for (int i = 0; i < imageWidth + imageHeight; i += 2 * axonometricWidth) {
			cr.MoveTo (0, i);
			cr.LineTo (i, 0);
		}

		// Adds descending diagonal lines.
		// The 'imageHeight % (2 * axonometricWidth)' adjustment is used to ensure our lines will align
		// to the top [0, 0] instead of the bottom [0, imageHeight]
		for (int i = -imageHeight + imageHeight % (2 * axonometricWidth); i < imageWidth; i += 2 * axonometricWidth) {
			cr.MoveTo (i, 0);
			cr.LineTo (i + imageHeight, imageHeight);
		}

		// Add vertical lines.
		for (int i = 0; i < imageWidth; i += axonometricWidth) {
			cr.MoveTo (i, 0);
			cr.LineTo (i, imageHeight);
		}

		cr.Stroke ();

		snapshot.Pop ();
	}

	/// <summary>
	/// The axonometric grid should be drawn if it is enabled and we're zoomed in far enough.
	/// </summary>
	private bool ShouldShowCanvasAxonometricGrid ()
	{
		if (!canvas_grid.ShowAxonometricGrid)
			return false;

		const int MIN_GRID_LINE_DISTANCE = 5;

		int minCanvasDistance = canvas_grid.AxonometricWidth;
		int minViewDistance = (int) Math.Ceiling (minCanvasDistance * document.Workspace.Scale);

		return minViewDistance >= MIN_GRID_LINE_DISTANCE;
	}

	/// <summary>
	/// Update the widget's size when the image size has changed, or e.g. zooming in.
	/// </summary>
	private void OnViewSizeChanged (object? o, System.EventArgs args)
	{
		Size viewSize = document.Workspace.ViewSize;
		SetSizeRequest (viewSize.Width, viewSize.Height);
	}

	#region Selection outline animation
	private bool SelectionAnimationTick ()
	{
		// Note: using ActiveDocumentOrDefault in case the callback is invoked after closing the document.
		if (PintaCore.Workspace.ActiveDocumentOrDefault != document || !document.Selection.Visible)
			return true;

		selection_animation_dash_offset -= 1f;
		if (selection_animation_dash_offset < 0f)
			selection_animation_dash_offset += 6f;

		QueueUpdate ();
		return true;
	}
	#endregion

	public override void Dispose ()
	{
		// Remove the animation event handler to avoid leaks
		GLib.Source.Remove (selection_animation_timer_id);

		base.Dispose ();
	}
}
