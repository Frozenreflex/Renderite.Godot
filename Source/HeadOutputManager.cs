using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class HeadOutputManager : Node3D
{
    public static HeadOutputManager Instance;

    private Viewport _viewport;
    private Camera3D _camera;

    public bool IsXR = false;
    private XROrigin3D _xrOrigin;
    private XRCamera3D _xrCamera;
    private List<XRController3D> _controllers = new();

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        _viewport = GetViewport();
        _camera = new Camera3D();
        AddChild(_camera);

        IsXR = XRServer.PrimaryInterface is not null;

        if (IsXR)
        {
            _xrOrigin = new XROrigin3D();
            AddChild(_xrOrigin);

            _xrCamera = new XRCamera3D();
            _xrOrigin.AddChild(_xrCamera);

            _controllers.Add(new XRController3D
            {
                Tracker = "left_hand",
                Pose = "default"
            });
            _controllers.Add(new XRController3D
            {
                Tracker = "right_hand",
                Pose = "default"
            });
            _xrOrigin.AddChild(_controllers[0]);
            _xrOrigin.AddChild(_controllers[1]);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public void Handle(FrameSubmitData submitData, RenderSpace renderSpace)
    {
        var rotateY = new Quaternion(Vector3.Up, Mathf.Pi);
        Transform = TransformHelpers.TransformFromTRS(renderSpace.RootPosition, renderSpace.RootRotation * rotateY, renderSpace.RootScale);
        if (renderSpace.OverridePosition)
            Transform = TransformHelpers.TransformFromTRS(renderSpace.OverridenPosition, renderSpace.OverridenRotation * rotateY, renderSpace.OverridenScale);
        _camera.Near = submitData.nearClip;
        _camera.Far = submitData.farClip;
        _camera.Fov = submitData.desktopFOV;

        if (IsXR)
        {
            _xrCamera.Near = submitData.nearClip;
            _xrCamera.Far = submitData.farClip;

            var vrActive = submitData.vrActive;
            _viewport.UseXR = vrActive;
            _xrCamera.Current = vrActive;
            _camera.Current = !vrActive;
        }
    }

    public VR_InputsState GetVRInputState()
    {
        if (!IsXR)
            return new VR_InputsState();
        return new VR_InputsState
        {
            userPresentInHeadset = true,
            headsetState = new HeadsetState
            {
                isTracking = true,
                position = _xrCamera.Position.ToRenderiteZflip(),
                rotation = _xrCamera.Quaternion.ToRenderiteZflip(),
                connectionType = HeadsetConnection.Wired,
                headsetManufacturer = "Godot",
                headsetModel = "Godot"
            },
            // TODO: Make a huge ass map of all the controller types and buttons, and handle skeletons, and multiple devices, and trackers... (I told you it's gonna be tedious)
            controllers = _controllers.Select(controller =>
                    (VR_ControllerState)new IndexControllerState
                    {
                        side = controller.GetTrackerHand() == XRPositionalTracker.TrackerHand.Left ? Chirality.Left : Chirality.Right,
                        isDeviceActive = true,
                        isTracking = true,
                        position = controller.Position.ToRenderiteZflip(),
                        rotation = controller.Quaternion.ToRenderiteZflip(),
                        deviceID = controller.Name,
                        deviceModel = "knuckles",
                        hasBoundHand = true,
                        handPosition = controller.Position.ToRenderiteZflip(),
                        handRotation = controller.Quaternion.ToRenderiteZflip(),
                        trigger = controller.GetFloat("trigger"),
                        triggerClick = controller.IsButtonPressed("trigger_click"),
                        grip = controller.GetFloat("grip"),
                        gripClick = controller.IsButtonPressed("grip_click"),
                        joystickRaw = controller.GetVector2("primary").ToRenderite()
                    }).ToList(),
            hands = _controllers.Select(controller =>
                new HandState
                {
                    uniqueId = controller.Name + "_hand",
                    chirality = controller.GetTrackerHand() == XRPositionalTracker.TrackerHand.Left ? Chirality.Left : Chirality.Right,
                    isDeviceActive = true,
                    isTracking = true,
                    tracksMetacarpals = false,
                    confidence = 1f,
                    wristPosition = controller.Position.ToRenderiteZflip(),
                    wristRotation = controller.Quaternion.ToRenderiteZflip(),
                    segmentPositions = Enumerable.Repeat(Vector3.Zero.ToRenderite(), 31).ToList(),
                    segmentRotations = Enumerable.Repeat(Quaternion.Identity.ToRenderite(), 31).ToList(),
                }).ToList(),
        };
    }
}
