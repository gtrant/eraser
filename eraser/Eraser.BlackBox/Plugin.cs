using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Forms;

namespace Eraser.BlackBox
{
	class Plugin
	{
		void Init()
		{
			//Initialise our crash handler
			BlackBox blackBox = BlackBox.Get();
		}

		public static void OnGUIIdle(object sender, EventArgs e)
		{
			Application.Idle -= OnGUIIdle;
			BlackBox blackBox = BlackBox.Get();

			bool allSubmitted = true;
			foreach (BlackBoxReport report in blackBox.GetDumps())
				if (!report.Submitted)
				{
					allSubmitted = false;
					break;
				}

			if (allSubmitted)
				return;

			BlackBoxMainForm form = new BlackBoxMainForm();
			form.Show();
		}
	}
}
