using Hazel;
using UnityEngine;

//Thanks EHR - https://github.com/Gurge44/EndlessHostRoles/blob/main/Modules/Extensions/HazelExtensions.cs


namespace TOHE.Modules
{
    public static class HazelExtensions
    {
        public static void Write(this MessageWriter writer, Vector2 vector) => NetHelpers.WriteVector2(vector, writer);

        public static void Write(this MessageWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        public static void Write(this MessageWriter writer, Color color)
        {
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        // -------------------------------------------------------------------------------------------------------------------------

        public static Vector2 ReadVector2(this MessageReader reader) => NetHelpers.ReadVector2(reader);

        public static Vector3 ReadVector3(this MessageReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            return new(x, y, z);
        }

        public static Color ReadColor(this MessageReader reader)
        {
            float r = reader.ReadSingle();
            float g = reader.ReadSingle();
            float b = reader.ReadSingle();
            float a = reader.ReadSingle();
            return new(r, g, b, a);
        }
    }
}
