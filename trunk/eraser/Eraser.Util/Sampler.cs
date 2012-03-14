/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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
using System.Linq;
using System.Text;

namespace Eraser.Util
{
	/// <summary>
	/// Provides functions to sample data.
	/// </summary>
	/// <typeparam name="T">The type of data to sample.</typeparam>
	public class Sampler
	{
		public void Add(double sample)
		{
			Samples.Add(new KeyValuePair<DateTime, double>(DateTime.Now, sample));
		}

		/// <summary>
		/// Resets the sampler. This is useful when the existing data is found to be biased.
		/// </summary>
		public void Reset()
		{
			Samples.Clear();
		}

		/// <summary>
		/// Gets the prediction interval for this data set.
		/// </summary>
		/// <param name="significanceLevel">The level of significance of the prediction.</param>
		/// <returns>Null if insufficient data to make a prediction is available.</returns>
		public Interval Predict(double significanceLevel)
		{
			if (Samples.Count < 2)
				return null;

			double mean = Samples.Average(sample => sample.Value);
			double variance = Math.Sqrt(Samples.Sum(
				sample => Math.Pow(sample.Value - mean, 2.0)) / (double)(Samples.Count - 1));
			double tPercentile = alglib.studenttdistr.invstudenttdistribution(
				Samples.Count - 1, (significanceLevel + 1) / 2);

			double interval = tPercentile * variance * Math.Sqrt(1 + (1.0 / Samples.Count));
			return new Interval(mean - interval, mean + interval);
		}

		/// <summary>
		/// Gets the outliers in the sample.
		/// </summary>
		/// <param name="significanceLevel">The level of significance for the prediction.</param>
		/// <returns>The list of samples which are outliers, or null if insufficient data is
		/// available to determine which samples are outliers.</returns>
		public IList<KeyValuePair<DateTime, double>> GetOutliers(double significanceLevel)
		{
			Interval interval = Predict(significanceLevel);
			if (interval == null)
				return null;

			return Samples.Where(sample => !interval.Within(sample.Value)).ToList();
		}

		/// <summary>
		/// The samples comprising this data set.
		/// </summary>
		private List<KeyValuePair<DateTime, double>> Samples =
			new List<KeyValuePair<DateTime, double>>();
	}

	/// <summary>
	/// Represents an interval.
	/// </summary>
	public class Interval
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="minimum">The lower bound of the interval.</param>
		/// <param name="maximum">The upper bound of the interval.</param>
		public Interval(double minimum, double maximum)
		{
			Minimum = minimum;
			Maximum = maximum;
		}

		/// <summary>
		/// Checks whether the given value is within this interval.
		/// </summary>
		/// <param name="value">The value to check.</param>
		/// <returns>True if the value is within this interval.</returns>
		public bool Within(double value)
		{
			return value >= Minimum && value <= Maximum;
		}

		/// <summary>
		/// The lower bound of the interval.
		/// </summary>
		public double Minimum { get; private set; }

		/// <summary>
		/// The upper bound of the interval.
		/// </summary>
		public double Maximum { get; private set; }
	}
}
