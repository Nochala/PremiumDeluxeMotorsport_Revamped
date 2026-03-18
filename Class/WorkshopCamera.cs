using System;
using System.Drawing;
using System.Reflection;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Tools;
using Control = GTA.Control;

namespace PDMCD4
{
    public enum CameraPosition
    {
        Car,
        Interior,
        Engine,
        RearEngine,
        Trunk,
        FrontBumper,
        RearBumper,
        Grille,
        Tank,
        Plaque,
        BackPlate,
        FrontPlate,
        Wheels,
        Hood,
        RearWindscreen,
        BikeExhaust,
        FrontMuguard,
        RearMuguard,
    }

    public enum CameraRotationMode
    {
        Around,
        FirstPerson,
    }

    public class WorkshopCamera
    {
        private Camera _mainCamera;
        private bool _isDragging;
        private PointF _dragOffset;
        private Entity _target;
        private Vector3 _targetPos;
        private CameraPosition _internalCameraPosition;
        private CameraRotationMode _rotationMode;
        private CameraClamp _cameraClamp;
        private float _cameraZoom;
        private bool _justSwitched;

        public bool IsLerping;
        private DateTime startTime;
        private float duration;
        private Vector3 startValuePosition;
        private Vector3 endValuePosition;
        private Vector3 startValueRotation;
        private Vector3 endValueRotation;

        public WorkshopCamera()
        {
            Camera.DeleteAllCameras();
        }

        public CameraPosition MainCameraPosition
        {
            get => _internalCameraPosition;
            set
            {
                OnCameraChange(value);
                _internalCameraPosition = value;
            }
        }

        public Vector3 Rotation => _mainCamera?.Rotation ?? Vector3.Zero;

        public CameraRotationMode RotationMode
        {
            get => _rotationMode;
            set => _rotationMode = value;
        }

        public float CameraZoom
        {
            get => _cameraZoom;
            set
            {
                if (_mainCamera != null)
                {
                    Vector3 dir = CutsceneManager.RotationToDirection(_mainCamera.Rotation);
                    _mainCamera.Position += dir * (_cameraZoom - value);
                }

                _cameraZoom = value;
            }
        }

        public CameraClamp CameraClamp
        {
            get => _cameraClamp;
            set => _cameraClamp = value;
        }

        public void Stop()
        {
            StopRenderingCamera();
            Camera.DeleteAllCameras();
            _mainCamera = null;
        }

        public void RepositionFor(Vehicle lowrider)
        {
            if (lowrider == null)
            {
                return;
            }

            Camera.DeleteAllCameras();
            _mainCamera = CreateScriptedCamera(
                lowrider.Position - lowrider.ForwardVector * 5.0f + new Vector3(0f, 0f, 1.5f),
                CutsceneManager.DirectionToRotation(lowrider.ForwardVector * -5.0f),
                GameplayCamera.FieldOfView);

            _mainCamera.PointAt(lowrider);
            _mainCamera.Position = Helper.CameraPos;
            _mainCamera.Rotation = Helper.CameraRot;
            StartRenderingCamera(_mainCamera);
            _target = lowrider;
            _targetPos = lowrider.Position;
            _cameraZoom = 5.0f;
            _internalCameraPosition = CameraPosition.Car;
            RotationMode = CameraRotationMode.Around;
            CameraClamp = new CameraClamp
            {
                MaxVerticalValue = -40.0f,
                MinVerticalValue = -3.0f,
            };
            _mainCamera.Shake(CameraShake.Hand, 0.5f);
        }

        public bool IsMouseInMenu()
        {
            PointF topLeft = SafeZone.GetSafePosition(new PointF(0f, 0f));
            SizeF size = new SizeF(431f / GameScreen.AbsoluteResolution.Width, 550f / GameScreen.AbsoluteResolution.Height);
            return GameScreen.IsCursorInArea(topLeft, size);
        }

        public void Update()
        {
            if (_mainCamera == null)
            {
                return;
            }

            Game.DisableControlThisFrame(Control.VehicleMouseControlOverride);

            if (IsLerping)
            {
                DateTime now = DateTime.Now;
                float elapsed = (float)now.Subtract(startTime).TotalMilliseconds;
                if (elapsed > duration)
                {
                    IsLerping = false;
                    _mainCamera.Position = endValuePosition;
                    _mainCamera.Rotation = endValueRotation;
                    return;
                }

                _mainCamera.Position = LerpVector(elapsed, duration, startValuePosition, endValuePosition);
                _mainCamera.Rotation = LerpVector(elapsed, duration, startValueRotation, endValueRotation);
                return;
            }

            if (_justSwitched)
            {
                _justSwitched = false;
                return;
            }

            if (Game.IsControlJustPressed(Control.Attack) && !_isDragging && !IsMouseInMenu())
            {
                _isDragging = true;
                float mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorX);
                float mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorY);
                Function.Call((Hash)0x8DB8CFFD58B62552UL, 4);
                mouseX = (mouseX * 2f) - 1f;
                mouseY = (mouseY * 2f) - 1f;
                _dragOffset = new PointF(mouseX, mouseY);
            }

            if (Game.IsControlJustReleased(Control.Attack) && _isDragging)
            {
                _isDragging = false;
                _dragOffset = PointF.Empty;
                Function.Call((Hash)0x8DB8CFFD58B62552UL, 0);
            }

            if (RotationMode == CameraRotationMode.Around)
            {
                UpdateAroundCamera();
            }
            else if (RotationMode == CameraRotationMode.FirstPerson)
            {
                UpdateFirstPersonCamera();
            }

            if (MainCameraPosition != CameraPosition.Interior)
            {
                Helper.CameraPos = _mainCamera.Position;
                Helper.CameraRot = _mainCamera.Rotation;
            }
        }

        private void UpdateAroundCamera()
        {
            if (_isDragging)
            {
                GTA.UI.Hud.ShowCursorThisFrame();
                Vector3 dir = CutsceneManager.RotationToDirection(_mainCamera.Rotation);
                float len = (_targetPos - _mainCamera.Position).Length();

                Vector3 rotLeft = _mainCamera.Rotation + new Vector3(0f, 0f, -10f);
                Vector3 rotRight = _mainCamera.Rotation + new Vector3(0f, 0f, 10f);
                Vector3 right = CutsceneManager.RotationToDirection(rotRight) - CutsceneManager.RotationToDirection(rotLeft);

                Vector3 rotUp = _mainCamera.Rotation + new Vector3(-20f, 0f, 0f);
                Vector3 rotDown = _mainCamera.Rotation + new Vector3(20f, 0f, 0f);
                Vector3 up = CutsceneManager.RotationToDirection(rotUp) - CutsceneManager.RotationToDirection(rotDown);

                float mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorX);
                float mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorY);
                mouseX = (mouseX * 2f) - 1f;
                mouseY = (mouseY * 2f) - 1f;

                Vector3 rotation = Vector3.Zero;
                if (!IsCameraClamped(true, mouseX - _dragOffset.X))
                {
                    rotation += right * 15f * (mouseX - _dragOffset.X);
                }
                if (!IsCameraClamped(false, mouseY - _dragOffset.Y))
                {
                    rotation += up * -((mouseY - _dragOffset.Y) * 15f);
                }
                rotation += dir * (len - CameraZoom);
                _mainCamera.Position += rotation;
                _dragOffset = new PointF(mouseX, mouseY);
            }

            if (Game.LastInputMethod == InputMethod.GamePad)
            {
                Vector3 dir = CutsceneManager.RotationToDirection(_mainCamera.Rotation);
                float len = (_targetPos - _mainCamera.Position).Length();

                Vector3 rotLeft = _mainCamera.Rotation + new Vector3(0f, 0f, -10f);
                Vector3 rotRight = _mainCamera.Rotation + new Vector3(0f, 0f, 10f);
                Vector3 right = CutsceneManager.RotationToDirection(rotRight) - CutsceneManager.RotationToDirection(rotLeft);

                Vector3 rotUp = _mainCamera.Rotation + new Vector3(-20f, 0f, 0f);
                Vector3 rotDown = _mainCamera.Rotation + new Vector3(20f, 0f, 0f);
                Vector3 up = CutsceneManager.RotationToDirection(rotUp) - CutsceneManager.RotationToDirection(rotDown);

                float mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookLeftRight);
                float mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookUpDown);
                Vector3 rotation = Vector3.Zero;

                if (!IsCameraClamped(true, mouseX))
                {
                    rotation += right * mouseX * 0.6f;
                }
                if (!IsCameraClamped(false, mouseY))
                {
                    rotation += up * -mouseY * 0.5f;
                }
                rotation += dir * (len - CameraZoom);
                _mainCamera.Position += rotation;
            }
        }

        private void UpdateFirstPersonCamera()
        {
            if (_isDragging)
            {
                GTA.UI.Hud.ShowCursorThisFrame();
                float mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorX);
                float mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorY);
                mouseX = (mouseX * 2f) - 1f;
                mouseY = ((mouseY * 2f) - 1f) * -1f;

                Vector3 right = new Vector3(0f, 0f, 1f);
                Vector3 up = new Vector3(1f, 0f, 0f);
                Vector3 rotation = Vector3.Zero;

                if (!IsCameraClamped(true, mouseX - _dragOffset.X))
                {
                    rotation += right * 20f * (mouseX - _dragOffset.X);
                }
                if (!IsCameraClamped(false, mouseY - _dragOffset.Y))
                {
                    rotation += up * -((mouseY - _dragOffset.Y) * 20f);
                }
                _mainCamera.Rotation += rotation;
                _dragOffset = new PointF(mouseX, mouseY);
            }

            if (Game.LastInputMethod == InputMethod.GamePad)
            {
                float mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookLeftRight) * -1f;
                float mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookUpDown);
                Vector3 right = new Vector3(0f, 0f, 1f);
                Vector3 up = new Vector3(1f, 0f, 0f);
                Vector3 rotation = Vector3.Zero;

                if (!IsCameraClamped(true, mouseX))
                {
                    rotation += right * mouseX * 4.0f;
                }
                if (!IsCameraClamped(false, mouseY))
                {
                    rotation += up * -mouseY * 4.0f;
                }
                _mainCamera.Rotation += rotation;
            }
        }

        public bool IsCameraClamped(bool horizontally, float delta)
        {
            if (_mainCamera == null || CameraClamp == null)
            {
                return false;
            }

            if (horizontally)
            {
                bool goingLeft = delta < 0f;
                float left = CameraClamp.LeftHorizontalValue;
                float right = CameraClamp.RightHorizontalValue;

                if (left > 180f)
                {
                    left -= 360f * ((int)(left / 360f) + 1);
                }
                if (right > 180f)
                {
                    right -= 360f * ((int)(right / 360f) + 1);
                }

                bool sameHemisphereLeft = (_mainCamera.Rotation.Z > 0f && left > 0f) || (_mainCamera.Rotation.Z < 0f && left < 0f);
                bool sameHemisphereRight = (_mainCamera.Rotation.Z > 0f && right > 0f) || (_mainCamera.Rotation.Z < 0f && right < 0f);

                if (goingLeft && _mainCamera.Rotation.Z > right && sameHemisphereRight)
                {
                    return true;
                }
                if (!goingLeft && _mainCamera.Rotation.Z < left && sameHemisphereLeft)
                {
                    return true;
                }
                return false;
            }
            else
            {
                bool goingDown = delta < 0f;
                if (goingDown && _mainCamera.Rotation.X > CameraClamp.MinVerticalValue)
                {
                    return true;
                }
                if (!goingDown && _mainCamera.Rotation.X < CameraClamp.MaxVerticalValue)
                {
                    return true;
                }
                return false;
            }
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value > max)
            {
                return max;
            }
            if (value < min)
            {
                return min;
            }
            return value;
        }

        public static Vector3 GetBonePosition(Entity target, string bone)
        {
            return target.Bones[bone].Position;
        }

        public static float QuadraticEasing(float currentTime, float startValue, float changeInValue, float duration)
        {
            currentTime /= duration / 2f;
            if (currentTime < 1f)
            {
                return changeInValue / 2f * currentTime * currentTime + startValue;
            }
            currentTime -= 1f;
            return -changeInValue / 2f * (currentTime * (currentTime - 2f) - 1f) + startValue;
        }

        public static Vector3 LerpVector(float currentTime, float duration, Vector3 startValue, Vector3 destination)
        {
            return new Vector3(
                QuadraticEasing(currentTime, startValue.X, destination.X - startValue.X, duration),
                QuadraticEasing(currentTime, startValue.Y, destination.Y - startValue.Y, duration),
                QuadraticEasing(currentTime, startValue.Z, destination.Z - startValue.Z, duration));
        }

        private void StartLerp(Vector3 endPosition, Vector3 endRotation)
        {
            startValueRotation = _mainCamera.Rotation;
            startValuePosition = _mainCamera.Position;
            duration = 1000.0f;
            IsLerping = true;
            startTime = DateTime.Now;
            endValuePosition = endPosition;
            endValueRotation = endRotation;
        }

        private void SetAroundCamera(Vector3 targetPos, float zoom, Vector3 endPosition, Vector3 endRotation, CameraClamp clamp, bool pointAtTarget = true)
        {
            Game.Player.Character.Opacity = 255;
            RotationMode = CameraRotationMode.Around;
            _targetPos = targetPos;
            _cameraZoom = zoom;
            if (pointAtTarget)
            {
                _mainCamera.StopPointing();
                _mainCamera.PointAt(_targetPos);
            }
            CameraClamp = clamp;
            StartLerp(endPosition, endRotation);
            _justSwitched = true;
        }

        private static Camera CreateScriptedCamera(Vector3 position, Vector3 rotation, float fieldOfView)
        {
            try
            {
                MethodInfo createMethod = typeof(Camera).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
                if (createMethod != null)
                {
                    ParameterInfo[] parameters = createMethod.GetParameters();
                    if (parameters.Length == 3 && parameters[0].ParameterType.IsEnum)
                    {
                        Type enumType = parameters[0].ParameterType;
                        object camHash;

                        string[] preferredNames = new[]
                        {
                            "DefaultScriptedCamera",
                            "DEFAULT_SCRIPTED_CAMERA",
                            "Default",
                        };

                        camHash = null;
                        foreach (string name in preferredNames)
                        {
                            if (Enum.IsDefined(enumType, name))
                            {
                                camHash = Enum.Parse(enumType, name);
                                break;
                            }
                        }

                        if (camHash == null)
                        {
                            Array values = Enum.GetValues(enumType);
                            if (values.Length > 0)
                            {
                                camHash = values.GetValue(0);
                            }
                        }

                        if (camHash != null)
                        {
                            Camera camera = (Camera)createMethod.Invoke(null, new object[] { camHash, position, rotation });
                            if (camera != null)
                            {
                                camera.FieldOfView = fieldOfView;
                                return camera;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            int handle = Function.Call<int>(Hash.CREATE_CAM, "DEFAULT_SCRIPTED_CAMERA", true);
            Camera fallbackCamera = new Camera(handle)
            {
                Position = position,
                Rotation = rotation,
                FieldOfView = fieldOfView,
            };
            return fallbackCamera;
        }

        private static void StartRenderingCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            try
            {
                camera.IsActive = true;
            }
            catch
            {
            }

            try
            {
                MethodInfo[] methods = typeof(ScriptCameraDirector).GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "StartRendering")
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        method.Invoke(null, null);
                        return;
                    }
                    if (parameters.Length == 1)
                    {
                        Type paramType = parameters[0].ParameterType;
                        if (paramType == typeof(bool))
                        {
                            method.Invoke(null, new object[] { true });
                            return;
                        }
                        if (paramType.IsAssignableFrom(typeof(Camera)))
                        {
                            method.Invoke(null, new object[] { camera });
                            return;
                        }
                    }
                }
            }
            catch
            {
            }

            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, false, 0, true, false, 0);
        }

        private static void StopRenderingCamera()
        {
            try
            {
                MethodInfo[] methods = typeof(ScriptCameraDirector).GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (MethodInfo method in methods)
                {
                    if (method.Name == "StopRendering" && method.GetParameters().Length == 0)
                    {
                        method.Invoke(null, null);
                        return;
                    }
                }
            }
            catch
            {
            }

            Function.Call(Hash.RENDER_SCRIPT_CAMS, false, false, 0, true, false, 0);
        }

        private void OnCameraChange(CameraPosition newPos)
        {
            if (_mainCamera == null || _target == null)
            {
                return;
            }

            switch (newPos)
            {
                case CameraPosition.Car:
                    Game.Player.Character.Opacity = 255;
                    RotationMode = CameraRotationMode.Around;
                    if (_internalCameraPosition != CameraPosition.Car && _internalCameraPosition != CameraPosition.Interior)
                    {
                        _mainCamera.PointAt(_target);
                        _targetPos = _target.Position;
                        _cameraZoom = 5.0f;
                        CameraClamp = new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f };
                        _mainCamera.Shake(CameraShake.Hand, 0.5f);
                        StartLerp(startValuePosition, startValueRotation);
                    }
                    else if (_internalCameraPosition == CameraPosition.Interior)
                    {
                        RepositionFor((Vehicle)_target);
                    }
                    break;
                case CameraPosition.Wheels:
                    Game.Player.Character.Opacity = 255;
                    RotationMode = CameraRotationMode.Around;
                    if (_internalCameraPosition != CameraPosition.Car)
                    {
                        RepositionFor((Vehicle)_target);
                    }
                    CameraClamp = new CameraClamp
                    {
                        MaxVerticalValue = -60.0f,
                        MinVerticalValue = -3.0f,
                        LeftHorizontalValue = _target.Heading - 130.0f,
                        RightHorizontalValue = _target.Heading - 380.0f,
                    };
                    _cameraZoom = 4.0f;
                    StartLerp(_target.Position - _target.RightVector * 4.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading - 90.0f));
                    break;
                case CameraPosition.Interior:
                    IsLerping = false;
                    Vector3 headPos = GetBonePosition(_target, "seat_dside_f");
                    Camera.DeleteAllCameras();
                    _mainCamera = CreateScriptedCamera(headPos + new Vector3(0.2f, 0.3f, 0.6f), new Vector3(0f, 0f, _target.Heading), GameplayCamera.FieldOfView);
                    StartRenderingCamera(_mainCamera);
                    _targetPos = headPos;
                    RotationMode = CameraRotationMode.FirstPerson;
                    Game.Player.Character.Opacity = 255;
                    CameraClamp = new CameraClamp { MaxVerticalValue = -60.0f, MinVerticalValue = -3.0f };
                    _justSwitched = true;
                    break;
                case CameraPosition.Engine:
                    SetAroundCamera(GetBonePosition(_target, "engine"), 3.0f, _targetPos + _target.ForwardVector * 3.0f + _target.UpVector, new Vector3(0f, 0f, -_target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 250.6141f, RightHorizontalValue = _target.Heading - 105.30705f });
                    break;
                case CameraPosition.Hood:
                    SetAroundCamera(GetBonePosition(_target, "bonnet"), 3.0f, _targetPos + _target.ForwardVector * 3.0f + _target.UpVector, new Vector3(0f, 0f, -_target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 250.6141f, RightHorizontalValue = _target.Heading - 105.30705f });
                    break;
                case CameraPosition.FrontMuguard:
                    _targetPos = _target.Bones.Contains("misc_i") ? GetBonePosition(_target, "misc_i") : GetBonePosition(_target, "forks_l");
                    SetAroundCamera(_targetPos, 3.0f, _targetPos + _target.ForwardVector * 3.0f + _target.UpVector, new Vector3(0f, 0f, -_target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 250.6141f, RightHorizontalValue = _target.Heading - 105.30705f });
                    break;
                case CameraPosition.Trunk:
                    _targetPos = _target.Bones.Contains("boot") ? GetBonePosition(_target, "boot") : GetBonePosition(_target, "bumper_r");
                    SetAroundCamera(_targetPos, 3.0f, _targetPos + _target.ForwardVector * -3.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.BikeExhaust:
                    _targetPos = GetBonePosition(_target, "exhaust");
                    SetAroundCamera(_targetPos, 3.0f, _targetPos + _target.ForwardVector * -3.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.RearMuguard:
                    _targetPos = GetBonePosition(_target, "misc_d");
                    SetAroundCamera(_targetPos, 3.0f, _targetPos + _target.ForwardVector * -3.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.RearWindscreen:
                    _targetPos = _target.Bones.Contains("windscreen_r") ? GetBonePosition(_target, "windscreen_r") : GetBonePosition(_target, "bumper_r");
                    SetAroundCamera(_targetPos, 3.0f, _targetPos + _target.ForwardVector * -3.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.RearEngine:
                    _targetPos = GetBonePosition(_target, "engine");
                    SetAroundCamera(_targetPos, 3.0f, _targetPos + _target.ForwardVector * -3.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.FrontBumper:
                    _targetPos = GetBonePosition(_target, "neon_f");
                    SetAroundCamera(_targetPos, 2.0f, _targetPos + _target.ForwardVector * 2.0f + _target.UpVector, new Vector3(0f, 0f, -_target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 250.6141f, RightHorizontalValue = _target.Heading - 105.30705f });
                    break;
                case CameraPosition.Grille:
                    _targetPos = GetBonePosition(_target, "neon_f");
                    SetAroundCamera(_targetPos, 2.0f, _targetPos + _target.ForwardVector * 2.0f + _target.UpVector * 3.0f, new Vector3(0f, 0f, -_target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 250.6141f, RightHorizontalValue = _target.Heading - 105.30705f });
                    break;
                case CameraPosition.RearBumper:
                    _targetPos = GetBonePosition(_target, "neon_b");
                    SetAroundCamera(_targetPos, 2.0f, _targetPos + _target.ForwardVector * -2.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.Tank:
                    _targetPos = GetBonePosition(_target, "neon_b");
                    SetAroundCamera(_targetPos, 2.0f, _targetPos + _target.ForwardVector * -2.0f, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.Plaque:
                    string modelName = Helper.VehPreview != null ? Helper.VehPreview.Model.ToString().ToLowerInvariant() : string.Empty;
                    switch (modelName)
                    {
                        case "buccaneer2":
                        case "faction2":
                        case "moonbeam2":
                        case "slamvan3":
                        case "faction3":
                            _targetPos = GetBonePosition(_target, "misc_h");
                            break;
                        case "voodoo":
                        case "chino2":
                            _targetPos = GetBonePosition(_target, "misc_j");
                            break;
                        case "primo2":
                            _targetPos = GetBonePosition(_target, "misc_d");
                            break;
                        case "sabregt2":
                        case "virgo2":
                            _targetPos = GetBonePosition(_target, "misc_n");
                            break;
                        case "tornado5":
                            _targetPos = GetBonePosition(_target, "misc_o");
                            break;
                        case "minivan2":
                            _targetPos = GetBonePosition(_target, "misc_c");
                            break;
                        default:
                            _targetPos = GetBonePosition(_target, "windscreen_r");
                            break;
                    }
                    _cameraZoom = 0.6f;
                    float tRot = _target.Heading;
                    if (tRot > 180.0f)
                    {
                        tRot -= 360.0f;
                    }
                    CameraClamp = new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -20.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f };
                    StartLerp(_targetPos + _target.ForwardVector * -0.5f + _target.UpVector * 0.1f, new Vector3(0f, 0f, tRot));
                    _mainCamera.PointAt(_targetPos);
                    _justSwitched = true;
                    break;
                case CameraPosition.BackPlate:
                    _targetPos = _target.Bones.Contains("platelight") ? GetBonePosition(_target, "platelight") : GetBonePosition(_target, "neon_b");
                    SetAroundCamera(_targetPos, 1.0f, _targetPos + _target.ForwardVector * -1.0f + _target.UpVector, new Vector3(0f, 0f, _target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 60.0f, RightHorizontalValue = _target.Heading - 300.0f });
                    break;
                case CameraPosition.FrontPlate:
                    _targetPos = GetBonePosition(_target, "neon_f");
                    SetAroundCamera(_targetPos, 1.0f, _targetPos + _target.ForwardVector * 2.0f + _target.UpVector * 2.0f, new Vector3(0f, 0f, -_target.Heading), new CameraClamp { MaxVerticalValue = -40.0f, MinVerticalValue = -3.0f, LeftHorizontalValue = _target.Heading - 250.6141f, RightHorizontalValue = _target.Heading - 105.30705f });
                    break;
            }
        }
    }
}
