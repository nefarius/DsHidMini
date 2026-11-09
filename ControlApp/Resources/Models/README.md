# Motion viewer controller

`MotionController.obj` is an original, stylized controller mesh created for
DsHidMini. It uses the repository's BSD-3-Clause license (see `/LICENSE`).
No downloaded meshes, textures, fonts, or third-party assets are included.
It is not an exact CAD reproduction of a Sony controller.

The editable source is `build/generate_motion_controller.py`. Regenerate with
Python 3 from the repository root:

```sh
python build/generate_motion_controller.py
```

Generation uses only the Python standard library and produces deterministic OBJ
geometry with explicit smooth vertex normals. The checked-in mesh is embedded
in the application; Python and external files are not needed at runtime.

Coordinates are X right, +Y toward the USB port/shoulders, and +Z out of the
button face. The mesh is centred around the existing motion rotation origin.
Do not change the pose mapping to compensate for a modelling change.

OBJ group names select materials in `Models/Motion/MotionControllerModel.cs`.
Buttons, sticks, shell seam, shoulders, USB recess and face symbols are geometry,
so the viewer does not require texture loading or an additional model importer.
The red LED is a static orientation detail, not live player-slot telemetry.
