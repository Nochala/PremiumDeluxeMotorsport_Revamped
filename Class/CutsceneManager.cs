using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace PDMCD4
{
    public static class CutsceneManager
    {
        public static double BoundRotationDeg(double angleDeg)
        {
            int num = (int)(angleDeg / 360);
            double num2 = angleDeg - (num * 360);
            if (num2 < 0)
            {
                num2 += 360;
            }

            return num2;
        }

        public static Vector3 CrossWith(Vector3 left, Vector3 right)
        {
            return new Vector3(
                (left.Y * right.Z) - (left.Z * right.Y),
                (left.Z * right.X) - (left.X * right.Z),
                (left.X * right.Y) - (left.Y * right.X));
        }

        public static double DegToRad(double deg) => (deg * Math.PI) / 180d;

        public static Vector3 DirectionToRotation(Vector3 direction)
        {
            direction.Normalize();
            double x = Math.Atan2(direction.Z, direction.Y);
            double z = -Math.Atan2(direction.X, direction.Y);

            return new Vector3(
                (float)RadToDeg(x),
                0f,
                (float)RadToDeg(z));
        }

        public static Vector3 ForwardVector(Vector3 vector, float yaw)
        {
            Vector3 vector2 = Vector3.Zero;
            float num = (float)Math.Cos(yaw + 1.5707963267948966d);
            vector2.X = 57.29578f * num;
            vector2.Y = 0f;
            float num2 = (float)Math.Sin(yaw + 1.5707963267948966d);
            vector2.Z = 57.29578f * num2;
            return CrossWith(vector, vector2);
        }

        public static double RadToDeg(double deg) => (deg * 180d) / Math.PI;

        public static Entity RaycastEntity(Vector2 screenCoord, Vector3 camPos, Vector3 camRot)
        {
            Vector3 origin = camPos;
            Entity ignoredEntity = Game.Player.Character;
            Vector3 direction = ScreenRelToWorld(camPos, camRot, screenCoord) - origin;
            direction.Normalize();

            RaycastResult result = World.Raycast(
                origin + (direction * 1.0f),
                origin + (direction * 100.0f),
                IntersectFlags.Foliage | IntersectFlags.Objects | IntersectFlags.Peds | IntersectFlags.Vehicles | IntersectFlags.Map,
                ignoredEntity);

            if (result.HitEntity != null)
            {
                return result.HitEntity;
            }

            return null;
        }

        public static Vector3 RaycastEverything(Vector2 screenCoord)
        {
            Vector3 position = GameplayCamera.Position;
            Vector3 rotation = GameplayCamera.Rotation;
            Vector3 target = ScreenRelToWorld(position, rotation, screenCoord);
            Vector3 origin = position;
            Entity ignoredEntity = Game.Player.Character;

            if (Game.Player.Character.IsInVehicle())
            {
                ignoredEntity = Game.Player.Character.CurrentVehicle;
            }

            Vector3 direction = target - origin;
            direction.Normalize();

            RaycastResult result = World.Raycast(
                origin + (direction * 1.0f),
                origin + (direction * 100.0f),
                IntersectFlags.Foliage | IntersectFlags.Objects | IntersectFlags.Peds | IntersectFlags.Vehicles | IntersectFlags.Map,
                ignoredEntity);

            if (result.DidHit)
            {
                return result.HitPosition;
            }

            return position + (direction * 100.0f);
        }

        public static Vector3 RaycastEverything(Vector2 screenCoord, Vector3 camPos, Vector3 camRot, Entity toIgnore)
        {
            Vector3 origin = camPos;
            Vector3 direction = ScreenRelToWorld(camPos, camRot, screenCoord) - origin;
            direction.Normalize();

            RaycastResult result = World.Raycast(
                origin + (direction * 1.0f),
                origin + (direction * 100.0f),
                IntersectFlags.Foliage | IntersectFlags.Objects | IntersectFlags.Peds | IntersectFlags.Vehicles | IntersectFlags.Map,
                toIgnore);

            if (result.DidHit)
            {
                return result.HitPosition;
            }

            return camPos + (direction * 100.0f);
        }

        public static Vector3 RotationToDirection(Vector3 rotation)
        {
            double a = DegToRad(rotation.Z);
            double d = DegToRad(rotation.X);
            double num3 = Math.Abs(Math.Cos(d));

            return new Vector3(
                (float)(-Math.Sin(a) * num3),
                (float)(Math.Cos(a) * num3),
                (float)Math.Sin(d));
        }

        public static Vector3 ScreenRelToWorld(Vector3 camPos, Vector3 camRot, Vector2 coord)
        {
            Vector3 vector = RotationToDirection(camRot);
            Vector3 rotation = camRot + new Vector3(10.0f, 0f, 0f);
            Vector3 vector3 = camRot + new Vector3(-10.0f, 0f, 0f);
            Vector3 vector4 = camRot + new Vector3(0f, 0f, -10.0f);
            Vector3 vector5 = RotationToDirection(rotation) - RotationToDirection(vector3);
            double d = -DegToRad(camRot.Y);
            Vector3 vector1 = RotationToDirection(camRot + new Vector3(0f, 0f, 10.0f)) - RotationToDirection(vector4);
            Vector3 vector6 = (vector1 * (float)Math.Cos(d)) - (vector5 * (float)Math.Sin(d));
            Vector3 vector7 = (vector1 * (float)Math.Sin(d)) + (vector5 * (float)Math.Cos(d));

            if (!WorldToScreenRel(((camPos + (vector * 10.0f)) + vector6) + vector7, out Vector2 vector8))
            {
                return camPos + (vector * 10.0f);
            }

            if (!WorldToScreenRel(camPos + (vector * 10.0f), out Vector2 vector9))
            {
                return camPos + (vector * 10.0f);
            }

            if (Math.Abs(vector8.X - vector9.X) < 0.001f || Math.Abs(vector8.Y - vector9.Y) < 0.001f)
            {
                return camPos + (vector * 10.0f);
            }

            float num2 = (coord.X - vector9.X) / (vector8.X - vector9.X);
            float num3 = (coord.Y - vector9.Y) / (vector8.Y - vector9.Y);
            return ((camPos + (vector * 10.0f)) + (vector6 * num2)) + (vector7 * num3);
        }

        public static bool WorldToScreenRel(Vector3 worldCoords, out Vector2 screenCoords)
        {
            OutputArgument argument = new OutputArgument();
            OutputArgument argument2 = new OutputArgument();

            bool success = Function.Call<bool>((Hash)0x34E82F05DF2974F5, worldCoords.X, worldCoords.Y, worldCoords.Z, argument, argument2);
            if (!success)
            {
                screenCoords = new Vector2();
                return false;
            }

            screenCoords = new Vector2(
                (argument.GetResult<float>() - 0.5f) * 2.0f,
                (argument2.GetResult<float>() - 0.5f) * 2.0f);

            return true;
        }
    }
}
