using System.Windows.Media;
using System.Windows.Media.Media3D;

using HelixToolkit.Wpf;

using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class MotionViewerWindow
{
    private readonly AxisAngleRotation3D _pitch = CreateRotation(MotionViewerRotation.Pitch(0));
    private readonly AxisAngleRotation3D _roll = CreateRotation(MotionViewerRotation.Roll(0));
    private readonly AxisAngleRotation3D _yaw = CreateRotation(MotionViewerRotation.Yaw(0));
    private readonly MotionViewerViewModel _viewModel;

    public MotionViewerWindow(MotionViewerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        BuildPadModel();
        viewModel.PoseChanged += OnPoseChanged;
        Closed += async (_, _) =>
        {
            viewModel.PoseChanged -= OnPoseChanged;
            await viewModel.ShutdownAsync();
        };
        viewModel.Start();
    }

    private static AxisAngleRotation3D CreateRotation(MotionViewerAxisAngle mapped)
    {
        return new AxisAngleRotation3D(
            new Vector3D(mapped.AxisX, mapped.AxisY, mapped.AxisZ),
            mapped.AngleDegrees);
    }

    private void OnPoseChanged(object? sender, EventArgs e)
    {
        MotionViewerRotation.FromEuler(
            _viewModel.Estimator.PitchDegrees,
            _viewModel.Estimator.RollDegrees,
            _viewModel.Estimator.YawDegrees,
            out MotionViewerAxisAngle pitch,
            out MotionViewerAxisAngle roll,
            out MotionViewerAxisAngle yaw);
        _pitch.Angle = pitch.AngleDegrees;
        _roll.Angle = roll.AngleDegrees;
        _yaw.Angle = yaw.AngleDegrees;
    }

    private void BuildPadModel()
    {
        PadModel.Children.Add(new BoxVisual3D
        {
            Center = new Point3D(0, 0.15, 0),
            Length = 3.6,
            Width = 1.8,
            Height = 0.3,
            Fill = new SolidColorBrush(Colors.SteelBlue)
        });
        PadModel.Children.Add(new BoxVisual3D
        {
            Center = new Point3D(-1.7, -0.15, 0.15),
            Length = 0.9,
            Width = 1.3,
            Height = 0.45,
            Fill = new SolidColorBrush(Colors.DodgerBlue)
        });
        PadModel.Children.Add(new BoxVisual3D
        {
            Center = new Point3D(1.7, -0.15, 0.15),
            Length = 0.9,
            Width = 1.3,
            Height = 0.45,
            Fill = new SolidColorBrush(Colors.DodgerBlue)
        });
        PadModel.Children.Add(new BoxVisual3D
        {
            Center = new Point3D(0, 0.28, 0.55),
            Length = 1.1,
            Width = 0.45,
            Height = 0.12,
            Fill = new SolidColorBrush(Colors.LightSteelBlue)
        });

        Transform3DGroup transform = new();
        transform.Children.Add(new RotateTransform3D(_roll));
        transform.Children.Add(new RotateTransform3D(_pitch));
        transform.Children.Add(new RotateTransform3D(_yaw));
        PadModel.Transform = transform;

        Viewport.Children.Add(new ArrowVisual3D
        {
            Point1 = new Point3D(0, 0, 0),
            Point2 = new Point3D(2.4, 0, 0),
            Diameter = 0.06,
            Fill = Brushes.IndianRed
        });
        Viewport.Children.Add(new ArrowVisual3D
        {
            Point1 = new Point3D(0, 0, 0),
            Point2 = new Point3D(0, 2.0, 0),
            Diameter = 0.06,
            Fill = Brushes.YellowGreen
        });
        Viewport.Children.Add(new ArrowVisual3D
        {
            Point1 = new Point3D(0, 0, 0),
            Point2 = new Point3D(0, 0, 2.0),
            Diameter = 0.06,
            Fill = Brushes.CornflowerBlue
        });
    }
}
