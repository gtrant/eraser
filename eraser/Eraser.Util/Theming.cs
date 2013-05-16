/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By:
 * 
 * This file is part of Eraser.
 * 
 * Eraser is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 * 
 * Eraser is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * 
 * A copy of the GNU General Public License can be found at
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Windows.Forms.VisualStyles;
using Microsoft.Win32.SafeHandles;

namespace Eraser.Util
{
	public static class Theming
	{
		/// <summary>
		/// Verifies whether themeing is active.
		/// </summary>
		public static bool Active
		{
			get
			{
				try
				{
					return NativeMethods.IsThemeActive();
				}
				catch (FileLoadException)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <remarks>This function will also set the volume on all child controls.</remarks>
		public static void ApplyTheme(Control control)
		{
			ContainerControl container = control as ContainerControl;
			ButtonBase button = control as ButtonBase;
			ListView listview = control as ListView;
			ToolStrip toolstrip = control as ToolStrip;
			ComboBox combobox = control as ComboBox;

			if (container != null)
				container.Font = SystemFonts.MessageBoxFont;
			else if (control.Font != SystemFonts.MessageBoxFont)
				control.Font = new Font(SystemFonts.MessageBoxFont.FontFamily,
					control.Font.Size, control.Font.Style);

			if (button != null)
				ApplyTheme(button);
			else if (listview != null)
				ApplyTheme(listview);
			else if (combobox != null)
				ApplyTheme(combobox);
			else if (toolstrip != null)
				ApplyTheme(toolstrip);

			if (control.ContextMenuStrip != null)
				ApplyTheme(control.ContextMenuStrip);
			
			foreach (Control child in control.Controls)
				ApplyTheme(child);
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="button">The ButtonBase control to set the theme on.</param>
		public static void ApplyTheme(ButtonBase button)
		{
			if (button.FlatStyle == FlatStyle.Standard)
				button.FlatStyle = FlatStyle.System;
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="lv">The List View control to set the theme on.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void ApplyTheme(ListView lv)
		{
			try
			{
				NativeMethods.SetWindowTheme(lv.Handle, "EXPLORER", null);
				NativeMethods.SendMessage(lv.Handle, NativeMethods.LVM_SETEXTENDEDLISTVIEWSTYLE,
					(UIntPtr)NativeMethods.LVS_EX_DOUBLEBUFFER,
					(IntPtr)NativeMethods.LVS_EX_DOUBLEBUFFER);
			}
			catch (DllNotFoundException)
			{
			}
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="cb">The Combobox control to set the theme on.</param>
		public static void ApplyTheme(ComboBox cb)
		{
			//No themeing. I lied. This is to make focus behaviour work as expected.
			//Find all containers this belongs in and assign them a click handler.
			Control parent = cb.Parent;
			while (parent != null)
			{
				ScrollableControl container = parent as ScrollableControl;
				if (container != null && !ThemedContainers.Contains(container))
				{
					container.Click += OnContainerClicked;
					container.Disposed += OnContainerDisposed;
					ThemedContainers.Add(container);
				}
				parent = parent.Parent;
			}
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="menu">The tool strip control to set the theme on.</param>
		public static void ApplyTheme(ToolStrip menu)
		{
			//Register for Theme changed messages
			if (ThemeMessageFilter.Instance == null)
			{
				ThemeMessageFilter filter = new ThemeMessageFilter();
				filter.ThemeChanged += OnThemeChanged;
			}

			if (Environment.OSVersion.Version.Major >= 6)
			{
				//Assign our themed renderer for non-custom renderers
				UXThemeMenuRenderer renderer = new UXThemeMenuRenderer();
				if (menu.Renderer is ToolStripProfessionalRenderer)
				{
					menu.Disposed += OnThemedMenuDisposed;
					ThemedMenus.Add(menu, renderer);
					if (Active)
						menu.Renderer = renderer;
				}
			}

			foreach (ToolStripItem item in menu.Items)
			{
				ToolStripMenuItem toolStripItem = item as ToolStripMenuItem;
				if (toolStripItem != null)
					ApplyTheme(toolStripItem);
			}
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="menu">The List View control to set the theme on.</param>
		public static void ApplyTheme(ToolStripDropDownItem menuItem)
		{
			if (menuItem.Font != SystemFonts.MenuFont)
				menuItem.Font = new Font(SystemFonts.MenuFont, menuItem.Font.Style);

			ApplyTheme(menuItem.DropDown);
		}

		/// <summary>
		/// Handles the container clicked event. This is to act as a compatibility
		/// layer for combobox focus behaviour: if we click on a form, focus is not
		/// given to the form and scrolling would still remain in the combobox.
		/// </summary>
		private static void OnContainerClicked(object sender, EventArgs e)
		{
			ScrollableControl container = (ScrollableControl)sender;
			container.Focus();
		}

		/// <summary>
		/// Handles the theme changed event - reassigning the renderers to managed
		/// context menus.
		/// </summary>
		private static void OnThemeChanged(object sender, EventArgs e)
		{
			bool themesActive = Active;
			foreach (KeyValuePair<ToolStrip, UXThemeMenuRenderer> value in ThemedMenus)
			{
				if (themesActive)
					value.Key.Renderer = value.Value;
				else
					value.Key.RenderMode = ToolStripRenderMode.ManagerRenderMode;
			}
		}

		/// <summary>
		/// Clean up the reference to the container control when it is disposed
		/// so we allow garbage collection.
		/// </summary>
		private static void OnContainerDisposed(object sender, EventArgs e)
		{
			ThemedContainers.Remove((ScrollableControl)sender);
		}

		/// <summary>
		/// Clean up the reference to the menu when the menu is disposed so we no
		/// longer track the menu for theme changes.
		/// </summary>
		private static void OnThemedMenuDisposed(object sender, EventArgs e)
		{
			ThemedMenus.Remove(sender as ToolStrip);
		}

		/// <summary>
		/// The private list of menus which has their render changed to the UxTheme renderer.
		/// This allows us to revert the renderer back to the default Professional
		/// renderer when we get a theme changed message.
		/// </summary>
		private static Dictionary<ToolStrip, UXThemeMenuRenderer> ThemedMenus =
			new Dictionary<ToolStrip, UXThemeMenuRenderer>();

		/// <summary>
		/// The private list of containers which have comboboxes as one of their
		/// descendants. This is so that clicking outside the combobox will
		/// allow the focus to be lost. Users seem to expect this behaviour.
		/// </summary>
		private static HashSet<ScrollableControl> ThemedContainers =
			new HashSet<ScrollableControl>();

		/// <summary>
		/// Filters the Application message loop for WM_THEMECHANGED messages
		/// and broadcasts them to the event handlers.
		/// </summary>
		private class ThemeMessageFilter : IMessageFilter
		{
			public ThemeMessageFilter()
			{
				if (Instance != null)
					throw new InvalidOperationException("Only one instance of the " +
						"theme-change message filter can exist at any one time.");

				Instance = this;
				ThemesActive = Theming.Active;
				Application.AddMessageFilter(this);
			}

			#region IMessageFilter Members
			public bool PreFilterMessage(ref Message m)
			{
				if (m.Msg == NativeMethods.WM_THEMECHANGED)
				{
					ThemesActive = Theming.Active;
					ThemeChanged(null, EventArgs.Empty);
				}
				else if (m.Msg == NativeMethods.WM_DWMCOMPOSITIONCHANGED)
				{
					if (ThemesActive != Theming.Active)
					{
						ThemesActive = Theming.Active;
						ThemeChanged(null, EventArgs.Empty);
					}
				}

				return false;			
			}
			#endregion

			/// <summary>
			/// The global ThemeMessageFilter instance.
			/// </summary>
			public static ThemeMessageFilter Instance
			{
				get;
				private set;
			}

			/// <summary>
			/// Called when a WM_THEMECHANGED message is sent.
			/// </summary>
			public EventHandler<EventArgs> ThemeChanged
			{
				get;
				set;
			}

			private bool ThemesActive;
		}
	}

	public class UXThemeMenuRenderer : ToolStripRenderer
	{
		protected override void Initialize(ToolStrip toolStrip)
		{
			base.Initialize(toolStrip);
			ToolStrip = toolStrip;
			Renderer = new VisualStyleRenderer(VisualStyleElement.Button.PushButton.Default);

			//Hook the item added event to inflate the height of every item by 2px.
			ToolStrip.ItemAdded += new ToolStripItemEventHandler(OnToolStripItemAdded);
			foreach (ToolStripItem item in toolStrip.Items)
				item.Height += 2;
		}

		void OnToolStripItemAdded(object sender, ToolStripItemEventArgs e)
		{
			//Inflate the height of every item by 2px.
			e.Item.Height += 2;
		}

		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			Rectangle rect = e.AffectedBounds;
			
			Renderer.SetParameters(MenuPopupBackground);
			if (Renderer.IsBackgroundPartiallyTransparent())
				Renderer.DrawParentBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.ToolStrip);
			
			Renderer.DrawBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.AffectedBounds);
		}

		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			//Strange, borders are drawn after the content. So, clip to only the borders
			//so that the internals will be retained.
			Region oldClip = e.Graphics.Clip;
			Rectangle insideRect = e.ToolStrip.ClientRectangle;

			//The correct (Windows) size is actually 3px, but that will cut into our items.
			insideRect.Inflate(-2, -2);
			e.Graphics.ExcludeClip(insideRect);

			Renderer.SetParameters(MenuPopupBorders);
			Renderer.DrawBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.AffectedBounds);

			//Restore the old clipping.
			e.Graphics.IntersectClip(insideRect);
		}

		protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
		{
			//Compute the rectangle to draw the gutter.
			Rectangle rect = e.AffectedBounds;
			rect.X = 0;
			Size gutterImageSize = Renderer.GetPartSize(e.Graphics, ThemeSizeType.True);
			rect.Width = GutterWidth - gutterImageSize.Width + 1;

			Renderer.SetParameters(MenuPopupGutter);
			Renderer.DrawBackground(e.Graphics, rect);
		}

		protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
		{
			//Compute the rectangle of the background.
			Rectangle rect = e.Item.ContentRectangle;
			rect.Inflate(0, 1);

			Renderer.SetParameters(GetItemElement(e.Item));
			Renderer.DrawBackground(e.Graphics, rect, rect);
		}

		protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
		{
			//Get the size of the gutter image
			Renderer.SetParameters(MenuPopupGutter);
			Size gutterImageSize = Renderer.GetPartSize(e.Graphics, ThemeSizeType.True);

			Renderer.SetParameters(MenuSeparator);
			Renderer.DrawBackground(e.Graphics, new Rectangle(
				GutterWidth - gutterImageSize.Width, 0, e.ToolStrip.DisplayRectangle.Width,
				e.Item.Height));
		}

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
		{
			if (!(e.Item is ToolStripMenuItem))
			{
				base.OnRenderItemCheck(e);
				return;
			}

			//Get the menu item.
			ToolStripMenuItem item = (ToolStripMenuItem)e.Item;

			//Compute the rectangle for the background.
			Rectangle rect = e.Item.ContentRectangle;
			rect.Y = 0;
			rect.Size = new Size(item.Height, item.Height);

			//Draw the background
			Renderer.SetParameters(GetCheckBackgroundElement(item));
			Renderer.DrawBackground(e.Graphics, rect);

			//Compute the size of the checkmark
			rect.Inflate(-3, -3);
			
			//Draw the checkmark
			Renderer.SetParameters(GetCheckElement(item));
			Renderer.DrawBackground(e.Graphics, rect);
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			Renderer.SetParameters(GetItemElement(e.Item));
			if (e.Item.Owner.IsDropDown || e.Item.Owner is MenuStrip)
				e.TextColor = Renderer.GetColor(ColorProperty.TextColor);

			base.OnRenderItemText(e);
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			Renderer.SetParameters(GetSubmenuElement(e.Item));
			Renderer.DrawBackground(e.Graphics, e.ArrowRectangle);
		}

		private VisualStyleElement GetItemElement(ToolStripItem item)
		{
			return item.Selected ?
				(item.Enabled ?
					MenuPopupItemHot :
					MenuPopupItemDisabledHot) :
				(item.Enabled ?
					MenuPopupItem :
					MenuPopupItemDisabled);
		}

		private VisualStyleElement GetCheckBackgroundElement(ToolStripItem item)
		{
			return item.Enabled ? MenuPopupCheckBackground : MenuPopupCheckBackgroundDisabled;
		}

		private VisualStyleElement GetCheckElement(ToolStripMenuItem item)
		{
			return item.Checked ?
				(item.Enabled ? MenuPopupCheck : MenuPopupCheckDisabled) :
				MenuPopupBitmap;
		}

		private VisualStyleElement GetSubmenuElement(ToolStripItem item)
		{
			return item.Enabled ? MenuSubmenu : MenuSubmenuDisabled;
		}

		/// <summary>
		/// Gets the width of the gutter for images.
		/// </summary>
		private int GutterWidth
		{
			get
			{
				return ToolStrip.DisplayRectangle.Left;
			}
		}

		private ToolStrip ToolStrip;
		private VisualStyleRenderer Renderer;

		private static readonly string MenuClass = "MENU";

		private static VisualStyleElement MenuPopupBackground =
			VisualStyleElement.CreateElement(
				MenuClass, (int)NativeMethods.MENUPARTS.MENU_POPUPBACKGROUND, 0);
		private static VisualStyleElement MenuPopupBorders =
			VisualStyleElement.CreateElement(
				MenuClass, (int)NativeMethods.MENUPARTS.MENU_POPUPBORDERS, 0);

		private static VisualStyleElement MenuPopupItem =
			VisualStyleElement.CreateElement(MenuClass,
				(int)NativeMethods.MENUPARTS.MENU_POPUPITEM,
				(int)NativeMethods.POPUPITEMSTATES.MPI_NORMAL);
		private static VisualStyleElement MenuPopupItemHot =
			VisualStyleElement.CreateElement(MenuClass,
				(int)NativeMethods.MENUPARTS.MENU_POPUPITEM,
				(int)NativeMethods.POPUPITEMSTATES.MPI_HOT);
		private static VisualStyleElement MenuPopupItemDisabled =
			VisualStyleElement.CreateElement(MenuClass,
				(int)NativeMethods.MENUPARTS.MENU_POPUPITEM,
				(int)NativeMethods.POPUPITEMSTATES.MPI_DISABLED);
		private static VisualStyleElement MenuPopupItemDisabledHot =
			VisualStyleElement.CreateElement(MenuClass,
				(int)NativeMethods.MENUPARTS.MENU_POPUPITEM,
				(int)NativeMethods.POPUPITEMSTATES.MPI_DISABLEDHOT);

		private VisualStyleElement MenuPopupCheckBackground =
			VisualStyleElement.CreateElement(MenuClass,
			(int)NativeMethods.MENUPARTS.MENU_POPUPCHECKBACKGROUND,
			(int)NativeMethods.POPUPCHECKBACKGROUNDSTATES.MCB_NORMAL);
		private VisualStyleElement MenuPopupCheckBackgroundDisabled =
			VisualStyleElement.CreateElement(MenuClass,
			(int)NativeMethods.MENUPARTS.MENU_POPUPCHECKBACKGROUND,
			(int)NativeMethods.POPUPCHECKBACKGROUNDSTATES.MCB_NORMAL);

		private VisualStyleElement MenuPopupBitmap =
			VisualStyleElement.CreateElement(MenuClass,
			(int)NativeMethods.MENUPARTS.MENU_POPUPCHECK, 0);
		private VisualStyleElement MenuPopupCheck =
			VisualStyleElement.CreateElement(MenuClass,
			(int)NativeMethods.MENUPARTS.MENU_POPUPCHECK,
			(int)NativeMethods.POPUPCHECKSTATES.MC_CHECKMARKNORMAL);
		private VisualStyleElement MenuPopupCheckDisabled =
			VisualStyleElement.CreateElement(MenuClass,
			(int)NativeMethods.MENUPARTS.MENU_POPUPCHECK,
			(int)NativeMethods.POPUPCHECKSTATES.MC_CHECKMARKDISABLED);
		

		private static VisualStyleElement MenuPopupGutter =
			VisualStyleElement.CreateElement(MenuClass,
				(int)NativeMethods.MENUPARTS.MENU_POPUPGUTTER, 0);

		private VisualStyleElement MenuSeparator =
			VisualStyleElement.CreateElement(MenuClass,
				(int)NativeMethods.MENUPARTS.MENU_POPUPSEPARATOR, 0);

		private VisualStyleElement MenuSubmenu =
			VisualStyleElement.CreateElement(MenuClass,
			(int)NativeMethods.MENUPARTS.MENU_POPUPSUBMENU,
			(int)NativeMethods.POPUPSUBMENUSTATES.MSM_NORMAL);
		private VisualStyleElement MenuSubmenuDisabled =
			VisualStyleElement.CreateElement(MenuClass,
			(int)NativeMethods.MENUPARTS.MENU_POPUPSUBMENU,
			(int)NativeMethods.POPUPSUBMENUSTATES.MSM_DISABLED);
	}
}
