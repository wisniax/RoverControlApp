using Godot;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class DistanceIndicator : Control
	{

		float _distance = 0f;
		[Export]
		public int Bars { get; set; } = 3;
		[Export]
		public float MaxDistance { get; set; } = 100f;
		[Export]
		public Color PrimaryColor { get; set; } = Colors.LimeGreen;
		[Export]
		public Color SecondaryColor { get; set; } = Colors.Gray;


		public float Distance
		{
			get => _distance;
			set
			{
				_distance = value;
				QueueRedraw();
				// NotifyPropertyChanged if implementing INotifyPropertyChanged
			}
		}

		public override void _Draw()
		{
			float step = MaxDistance / Bars;
			Vector2 center = new Vector2(Size.X / 2, Size.Y);

			for(int i = 0; i < Bars; i++)
			{
				float radius = 20 + i * 15;
				float thickness = 6;

				Color col = (_distance >= (i + 1) * step) ? PrimaryColor : SecondaryColor;
				DrawArc(center, radius, Mathf.Pi, Mathf.Pi * 2, 32, col, thickness);
			}
		}

	}
}
