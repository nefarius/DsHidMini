using System.Windows.Threading;

using Media3D = System.Windows.Media.Media3D;

using Vector3 = System.Numerics.Vector3;

using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

using HelixToolkit.Geometry;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;


namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class MotionViewerWindow
{
    private readonly GroupModel3D _pad = new();
    private readonly MotionViewerViewModel _viewModel;
    private int _renderGeneration;
    private bool _effectsDisposed;

    public MotionViewerWindow(MotionViewerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        bool useSoftwareRendering = viewModel.UseSoftwareRendering;
        InitializeComponent();
        if (viewModel.UseSoftwareRendering != useSoftwareRendering)
        {
            viewModel.UseSoftwareRendering = useSoftwareRendering;
        }

        BuildScene();
        viewModel.PoseChanged += OnPoseChanged;
        viewModel.SoftwareRenderingChanged += OnSoftwareRenderingChanged;
        Loaded += (_, _) => Viewport.ZoomExtents(0);
        Closed += async (_, _) =>
        {
            viewModel.PoseChanged -= OnPoseChanged;
            viewModel.SoftwareRenderingChanged -= OnSoftwareRenderingChanged;
            ReleaseEffectsManager();
            await viewModel.ShutdownAsync();
        };
        ApplyRenderingMode();
        viewModel.Start();
    }

    private void OnSoftwareRenderingChanged(object? sender, EventArgs e)
    {
        try
        {
            ApplicationConfiguration.Instance.MotionViewerSoftwareRendering = _viewModel.UseSoftwareRendering;
            ApplicationConfiguration.Instance.Save();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to persist motion viewer software rendering.");
        }

        ApplyRenderingMode();
    }

    private void ApplyRenderingMode()
    {
        if (_effectsDisposed)
        {
            return;
        }

        int generation = ++_renderGeneration;
        DefaultEffectsManager? created = null;
        try
        {
            created = MotionViewerRendering.CreateEffectsManager(_viewModel.UseSoftwareRendering);
            IEffectsManager? previous = Viewport.EffectsManager;

            // Flip-model swap chains keep the HWND until the old device is released.
            // Replacing the effects manager on the same call stack fails with E_ACCESSDENIED.
            bool releaseSwapChainFirst = Viewport.IsLoaded && previous is not null;
            if (releaseSwapChainFirst)
            {
                Viewport.EffectsManager = null;
            }

            if (previous is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (!releaseSwapChainFirst)
            {
                Viewport.EffectsManager = created;
                return;
            }

            DefaultEffectsManager pending = created;
            created = null;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                if (_effectsDisposed || generation != _renderGeneration)
                {
                    pending.Dispose();
                    return;
                }

                float dpi = 1f;
                bool restoreDpi = false;
                try
                {
                    try
                    {
                        // Nulling the manager leaves the host's pixel size in place, and the
                        // next start multiplies by DPI again. Collapse to logical size first.
                        if (Viewport.RenderHost is HelixToolkit.SharpDX.Render.DX11RenderHostBase host
                            && host.DpiScale > 1.001f)
                        {
                            dpi = host.DpiScale;
                            host.DpiScale = 1f;
                            restoreDpi = true;
                        }

                        Viewport.EffectsManager = pending;
                    }
                    finally
                    {
                        if (restoreDpi
                            && Viewport.RenderHost is HelixToolkit.SharpDX.Render.DX11RenderHostBase host)
                        {
                            host.DpiScale = dpi;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failed to change motion viewer rendering mode.");
                    pending.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            created?.Dispose();
            Log.Logger.Error(ex, "Failed to change motion viewer rendering mode.");
        }
    }

    private void ReleaseEffectsManager()
    {
        if (_effectsDisposed)
        {
            return;
        }

        _renderGeneration++;
        _effectsDisposed = true;
        try
        {
            IEffectsManager? current = Viewport.EffectsManager;
            Viewport.EffectsManager = null;
            (current as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to release the motion viewer DirectX device.");
        }
    }

    private void OnPoseChanged(object? sender, EventArgs e)
    {
        MotionViewerQuaternion q = MotionViewerRotation.ComposeYawThenTilt(
            _viewModel.Estimator.YawDegrees,
            _viewModel.Estimator.UpX,
            _viewModel.Estimator.UpY,
            _viewModel.Estimator.UpZ);
        _pad.Transform = new Media3D.RotateTransform3D(
            new Media3D.QuaternionRotation3D(new Media3D.Quaternion(q.X, q.Y, q.Z, q.W)));
        Viewport.InvalidateRender();
    }

    private void BuildScene()
    {
        // Look from the grips (-Y), with the face (+Z) visible. This places
        // the D-pad on the left and the shoulders at the top at rest, while
        // using the model frame mapped by MotionViewerRotation.
        Viewport.Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
        {
            Position = new Media3D.Point3D(2, -16, 20),
            LookDirection = new Media3D.Vector3D(-2, 16, -20),
            UpDirection = new Media3D.Vector3D(0, 0, 1),
            FieldOfView = 45,
            NearPlaneDistance = 0.1,
            FarPlaneDistance = 1000
        };
        _pad.Transform = Media3D.Transform3D.Identity;
        foreach (Object3D part in MotionControllerModel.Load())
        {
            _pad.Children.Add(new MeshGeometryModel3D
            {
                Geometry = part.Geometry,
                Material = MotionControllerModel.MaterialFor(part.Name)
            });
        }
        Viewport.Items.Add(_pad);
        Viewport.Items.Add(new LineGeometryModel3D
        {
            Geometry = LineBuilder.GenerateGrid(Vector3.UnitZ, -6, 6, -6, 6),
            Color = MediaColors.DimGray,
            Thickness = 1
        });
        AddArrow(new Vector3(2.4f, 0, 0), MediaColors.IndianRed);
        AddArrow(new Vector3(0, 2.0f, 0), MediaColors.YellowGreen);
        AddArrow(new Vector3(0, 0, 2.0f), MediaColors.CornflowerBlue);
    }

    private void AddArrow(Vector3 tip, MediaColor color)
    {
        Vector3 shaftEnd = tip * 0.82f;
        MeshBuilder builder = new();
        builder.AddCylinder(Vector3.Zero, shaftEnd, 0.03f, 16);
        builder.AddCone(shaftEnd, tip, 0.07f, true, 16);
        Viewport.Items.Add(new MeshGeometryModel3D
        {
            Geometry = builder.ToMeshGeometry3D(),
            Material = CreateMaterial(color)
        });
    }

    private static PhongMaterial CreateMaterial(MediaColor color)
    {
        Color4 diffuse = new(color.ScR, color.ScG, color.ScB, color.ScA);
        return new PhongMaterial
        {
            DiffuseColor = diffuse,
            AmbientColor = new Color4(diffuse.Red * 0.4f, diffuse.Green * 0.4f, diffuse.Blue * 0.4f, 1),
            SpecularColor = new Color4(0.25f, 0.25f, 0.25f, 1),
            SpecularShininess = 24
        };
    }
}
